using Core.Constants;
using Core.Constants.ProblemDetailsTitles;
using Core.Domain.BrokerAggregate;
using Core.Domain.BrokerAggregate.EmailConnection;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using SharedKernel.Exceptions;
using Web.Processing.EmailAutomation;
using Hangfire;
using Microsoft.Graph;


namespace Web.ControllerServices.QuickServices;

public class MSFTEmailQService
{
  private readonly AppDbContext _appDbContext;
  private readonly ADGraphWrapper _adGraphWrapper;
  private readonly IConfigurationSection _configurationSection;
  private readonly ILogger<MSFTEmailQService> _logger;
  public MSFTEmailQService(AppDbContext appDbContext, ADGraphWrapper aDGraph, IConfiguration config, ILogger<MSFTEmailQService> logger)
  {
    _appDbContext = appDbContext;
    _adGraphWrapper = aDGraph;
    _configurationSection = config.GetSection("URLs");
    _logger = logger;
  }

  /// <summary>
  /// Test if has access to tenant with this email and if yes subscribe to mailbox notifs
  /// Will thorw error if email already connected OR if no admin consent present
  /// </summary>
  public async Task ConnectEmail(Broker broker, string email, string TenantId,bool checkAdminConsent)
  {
    
    if (!broker.Agency.HasAdminEmailConsent && !checkAdminConsent)
    {
      if (broker.isAdmin)
        throw new CustomBadRequestException($"Admin has not consented to email permissions yet", ProblemDetailsTitles.StartAdminConsentFlow);
      else
        throw new CustomBadRequestException($"Admin has not consented to email permissions yet and current user is not an admin", ProblemDetailsTitles.AgencyAdminMustConsent);
    }

    var connectedEmails = _appDbContext.ConnectedEmails.Where(c => c.BrokerId == broker.Id).ToList();

    int emailNumber = 1;
    if (connectedEmails != null && connectedEmails.Count != 0)
    {
      foreach (var ConnEmail in connectedEmails)
      {
        if (ConnEmail.Email == email) throw new
            CustomBadRequestException($"the email {email} is already connected", ProblemDetailsTitles.EmailAlreadyConnected);
      }
      emailNumber= connectedEmails.Count+1;
    }
    
    var connectedEmail = new ConnectedEmail
    {
      BrokerId = broker.Id,
      Email = email,
      EmailNumber = emailNumber,
      EmailStatus = EmailStatus.Waiting,
      isMSFT = true,
    };
    var currDateTime = DateTime.UtcNow;
    //the maxinum subs period = just under 3 days
    DateTimeOffset SubsEnds = currDateTime + new TimeSpan(0, 4230, 0);

    var subs = new Subscription
    {
      ChangeType = "created",
      ClientState = VariousCons.MSFtWebhookSecret,
      ExpirationDateTime = SubsEnds,
      NotificationUrl = _configurationSection["MainAPI"] + "/MsftWebhook/Webhook",
      //Resource = $"users/{email}/mailFolders('inbox')/messages"
      Resource = $"users/{email}/messages"
    };
    _adGraphWrapper.CreateClient(TenantId);
    //will validate through the webhook before returning the subscription here
    var CreatedSubsTask = _adGraphWrapper._graphClient.Subscriptions.Request().AddAsync(subs);
    //TODO handle if we dont have adminConsent
    
    //TODO run the analyzer to sync? see how the notifs creator and email analyzer will work
    //will have to consider current leads in the system, current listings assigned, websites from which
    //emails will be parsed to detect new leads
    connectedEmail.FirstSync = currDateTime;
    connectedEmail.LastSync = currDateTime;

    var CreatedSubs = await CreatedSubsTask;
    connectedEmail.SubsExpiryDate = (DateTime)(CreatedSubs.ExpirationDateTime?.UtcDateTime);
    connectedEmail.GraphSubscriptionId = Guid.Parse(CreatedSubs.Id);

    //renew 60 minutes before subs Ends
    var renewalTime = SubsEnds - TimeSpan.FromMinutes(60);
    string RenewalJobId = BackgroundJob.Schedule<SubscriptionService>(s => s.RenewSubscription(broker.Id, emailNumber, TenantId), renewalTime);
    connectedEmail.SubsRenewalJobId = RenewalJobId;

    broker.ConnectedEmails = new List<ConnectedEmail>
    {
      connectedEmail
    };
    
    if (!broker.Agency.HasAdminEmailConsent) broker.Agency.HasAdminEmailConsent = true;
    await _appDbContext.SaveChangesAsync();
  }

  /// <summary>
  /// for now just sets current dateTime in connectedEmail
  /// </summary>
  /// <param name="connectedEmail"></param>
  /// <returns></returns>
  private async Task DoFirstEmailSyncAsync(ConnectedEmail connectedEmail)
  {
    

    //run the analyzer that will design later on.
  }

  /// <summary> 
  /// must call CreateCleint on class instance's _adGraphWrapper before
  /// </summary>
  /// <param name="connectedEmail"></param>
  /// <param name="graphClient"></param>
  /// <returns></returns>
  /*public async Task DoFirstEmailSync(ConnectedEmail connectedEmail)
  {
    connectedEmail.FolderSyncs = new();
    var SyncStartDate = DateTimeOffset.UtcNow;

    //Inbox syncing
    var inboxSync = HandleFirstFolderSync(connectedEmail, SyncStartDate, "Inbox");

    //SentItems syncing
    var sentSync = HandleFirstFolderSync(connectedEmail, SyncStartDate, "Sent Items");

    connectedEmail.FirstSync = SyncStartDate.UtcDateTime;
    connectedEmail.LastSync = SyncStartDate.UtcDateTime;
    await inboxSync;
    await sentSync;
  }*/
  /// <summary>
  /// To be primarily called by hangfire after a notif comes in
  /// For syncing
  /// </summary>
  /// <param name="ConnectedEmailId"></param>
  /// <param name="TenantId"></param>
  /// <returns></returns>
  public async Task SyncEmail(Guid subsId, string tenantId)
  {
    /*var connEmail = _appDbContext.ConnectedEmails.Single(e => e.GraphSubscriptionId == subsId);


    if(string.IsNullOrWhiteSpace(connEmail.FolderSyncToken))
    {

    }
    _adGraphWrapper.CreateClient(tenantId);

    List<Option> options = new List<Option>();

    options.Add(new QueryOption("$select", "sender,isRead,conversationId,conversationIndex,createdDateTime"));
    //options.Add(new QueryOption("$filter", "receivedDateTime+ge+{value}"));
    options.Add(new QueryOption("changeType", "created"));
    options.Add(new QueryOption("$orderby", "receivedDateTime+desc"));
    options.Add(new HeaderOption("Prefer: odata.maxpagesize", "30"));
    options.Add(new HeaderOption("Prefer: Prefer: IdType", "ImmutableId"));

    IMessageDeltaCollectionPage messages = await _adGraphWrapper._graphClient.Users[connEmail.Email]
      .MailFolders["inbox"].Messages.Delta()
      .Request(options).GetAsync();

    ProcessMessages(messages);

    while (messages.NextPageRequest != null)
    {
      //verify that header 30 max size exists
      messages = messages.NextPageRequest.GetAsync().Result;
      ProcessMessages(messages);
    }

    object? deltaLink;

    if (messages.AdditionalData.TryGetValue("@odata.deltaLink", out deltaLink))
    {
      connEmail.FolderSyncToken = deltaLink.ToString();
    }*/
  }



  /*public async Task HandleFirstFolderSync(ConnectedEmail connectedEmail, DateTimeOffset SyncStartDate, string FolderName)
  {
    var GraphClient = _adGraphWrapper._graphClient;

    var messages = await GetFirstMessagesPage(connectedEmail.Email, FolderName, SyncStartDate, true);

    var inboxSync1 = new FolderSync
    {
      FolderName = FolderName
    };
    connectedEmail.FolderSyncs.Add(inboxSync1);
    EmailHelpers.ProcessMessages(messages, _logger);

    while (messages.NextPageRequest != null)
    {
      //verify that header 30 max size exists
      messages = messages.NextPageRequest.GetAsync().Result;
      EmailHelpers.ProcessMessages(messages, _logger);
    }
    object? deltaLink;
    if (messages.AdditionalData.TryGetValue("@odata.deltaLink", out deltaLink))
    {
      inboxSync1.DeltaToken = deltaLink.ToString();
    }
    else
    {
      //TODO log error
    }
  }*/

  /// <summary>
  /// will also add caller's email as his connected email
  /// </summary>
  /// <param name="broker"></param>
  /// <param name="email"></param>
  /// <param name="TenantId"></param>
  /// <returns></returns>
  public async Task HandleAdminConsented(Broker broker, string email, string TenantId)
  {
    broker.Agency.HasAdminEmailConsent = true;
    broker.Agency.AzureTenantID = TenantId;

    await this.ConnectEmail(broker, email, TenantId,true);
  }

  /*public async Task<IMessageDeltaCollectionPage> GetFirstMessagesPage(string email, string folderName, DateTimeOffset SyncStartDate, bool startFresh)
  {
    List<Option> options = EmailHelpers.GetDeltaQueryOptions(SyncStartDate);
    IMessageDeltaCollectionPage messages = null;
    if (startFresh)
    {
      messages = await _adGraphWrapper._graphClient.Users[email]
      .MailFolders[folderName].Messages.Delta()
      .Request(options).GetAsync();
    }

    return messages;
  }*/
}
