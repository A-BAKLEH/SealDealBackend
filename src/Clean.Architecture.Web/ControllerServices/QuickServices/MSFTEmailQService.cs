using Clean.Architecture.Core.Constants;
using Clean.Architecture.Core.Constants.ProblemDetailsTitles;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate.EmailConnection;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Infrastructure.ExternalServices;
using Clean.Architecture.SharedKernel.Exceptions;
using Microsoft.Graph;


namespace Clean.Architecture.Web.ControllerServices.QuickServices;

public class MSFTEmailQService
{
  private readonly AppDbContext _appDbContext;
  private readonly ADGraphWrapper _adGraphWrapper;
  private readonly IConfigurationSection _configurationSection;
  private readonly ILogger<MSFTEmailQService> _logger;
  public MSFTEmailQService(AppDbContext appDbContext, ADGraphWrapper aDGraph, IConfiguration config,ILogger<MSFTEmailQService> logger)
  {
    _appDbContext = appDbContext;
    _adGraphWrapper = aDGraph;
    _configurationSection = config.GetSection("URLs");
    _logger = logger;
  }

  /// <summary>
  /// Test if has access to tenant with this email and if yes subscribe to mailbox notifs
  /// Will thorw error if email already connected
  /// </summary>
  public async Task ConnectEmail(Broker broker,string email, string TenantId)
  {
    if(!broker.Agency.HasAdminEmailConsent)
    {
      if(broker.isAdmin)
        throw new CustomBadRequestException($"Admin has not consented to email permissions yet", ProblemDetailsTitles.StartAdminConsentFlow);
      else
        throw new CustomBadRequestException($"Admin has not consented to email permissions yet and current user is not an admin", ProblemDetailsTitles.AgencyAdminMustConsent);
    }

    var connectedEmails = _appDbContext.ConnectedEmails.Where(c => c.BrokerId == broker.Id).ToList();

    foreach (var ConnEmail in connectedEmails)
    {
      if(ConnEmail.Email == email) throw new
          CustomBadRequestException($"the email {email} is already connected with good status", ProblemDetailsTitles.EmailAlreadyConnected);
    }
    var connectedEmail = new ConnectedEmail
    {
      BrokerId = broker.Id,
      Email = email,
      EmailNumber = connectedEmails.Count + 1,
      EmailStatus = EmailStatus.Waiting,
      isMSFT = true,
    };
    //the maxinum subs period = just under 3 days
    DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);

    var subs = new Subscription
    {
      ChangeType ="created",
      ClientState = VariousCons.MSFtWebhookSecret,
      ExpirationDateTime= SubsEnds,
      NotificationUrl = _configurationSection["MainAPI"]+ "/MsftWebhook/Webhook",
      //Resource = $"users/{email}/mailFolders('inbox')/messages"
      Resource = $"users/{email}/messages"
    };
    _adGraphWrapper.CreateClient(TenantId);
    //will validate through the webhook before returning the subscription here
    var CreatedSubsTask = _adGraphWrapper._graphClient.Subscriptions.Request().AddAsync(subs);

    //TODO test all error paths and handle them and re-try the request
    //possiblitites: db says we have admin consent we dont
    
    //creates delta tokens for inbox and Sent Items folders
    var SyncingTask = DoFirstEmailSync(connectedEmail);
    //Todo async error handling

    var CreatedSubs = await CreatedSubsTask;
    connectedEmail.SubsExpiryDate = (DateTime)(CreatedSubs.ExpirationDateTime?.UtcDateTime);
    connectedEmail.GraphSubscriptionId = Guid.Parse(CreatedSubs.Id);

    //TODO schedule hangire renewaljob
    string RenewalJobId = "";
    connectedEmail.SubsRenewalJobId = RenewalJobId;

    broker.ConnectedEmails = new List<ConnectedEmail>
    {
      connectedEmail
    };
    await SyncingTask;
    _appDbContext.SaveChanges();
  }

  //establish a start point for delta queries for folders 
  public async Task DoFirstEmailSync(ConnectedEmail connectedEmail)
  {
    connectedEmail.FolderSyncs = new();
    var SyncStartDate = DateTimeOffset.UtcNow;

    //Inbox syncing
    var inboxSync = HandleFirstFolderSync(connectedEmail, SyncStartDate,"Inbox");

    //SentItems syncing
    var sentSync = HandleFirstFolderSync(connectedEmail, SyncStartDate, "Sent Items");

    connectedEmail.FirstSync = SyncStartDate.UtcDateTime;
    connectedEmail.LastSync = SyncStartDate.UtcDateTime;
    await inboxSync;
    await sentSync;
  }
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

  private void ProcessMessages(IMessageDeltaCollectionPage messages)
  {
    throw new NotImplementedException();
  }

  public async Task HandleFirstFolderSync(ConnectedEmail connectedEmail, DateTimeOffset SyncStartDate,string FolderName)
  {
    List<Option> options = GetDeltaOptions(SyncStartDate);

    IMessageDeltaCollectionPage messages = await _adGraphWrapper._graphClient.Users[connectedEmail.Email]
      .MailFolders[FolderName].Messages.Delta()
      .Request(options).GetAsync();
    var inboxSync1 = new FolderSync
    {
      FolderName = FolderName
    };
    connectedEmail.FolderSyncs.Add(inboxSync1);
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
      inboxSync1.DeltaToken = deltaLink.ToString();
    }
    else
    {
      //TODO log error
    }
  }

  /*private async Task<IMessageDeltaCollectionPage> GetMessages(GraphServiceClient graphClient, object? deltaLink)
  {
    IUserDeltaCollectionPage page;

    if (lastPage == null || deltaLink == null)
    {
      page = await graphClient.Users
                              .Delta()
                              .Request()
                              .GetAsync();
    }
    else
    {
      lastPage.InitializeNextPageRequest(graphClient, deltaLink.ToString());
      page = await lastPage.NextPageRequest.GetAsync();
    }

    lastPage = page;
    return page;
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

    await this.ConnectEmail(broker, email, TenantId);
  }


 private List<Option> GetDeltaOptions(DateTimeOffset SyncStartDate)
  {
    List<Option> options = new List<Option>();

    options.Add(new QueryOption("$select", "sender,isRead,conversationId,conversationIndex,createdDateTime"));
    options.Add(new QueryOption("$filter", $"receivedDateTime+ge+{SyncStartDate}"));
    options.Add(new QueryOption("changeType", "created"));
    options.Add(new QueryOption("$orderby", "receivedDateTime+desc"));
    options.Add(new HeaderOption("Prefer: odata.maxpagesize", "20"));
    options.Add(new HeaderOption("Prefer: IdType", "ImmutableId"));
    return options;
  }
}
