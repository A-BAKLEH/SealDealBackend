using Core.Constants;
using Core.Domain.BrokerAggregate.EmailConnection;
using Hangfire;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;

namespace Web.Processing.EmailAutomation;

public class EmailProcessor
{
  private readonly AppDbContext _appDbContext;
  private readonly ILogger<EmailProcessor> _logger;
  private ADGraphWrapper _aDGraphWrapper;
  private readonly IConfigurationSection _configurationSection;
  public EmailProcessor(AppDbContext appDbContext, IConfiguration config, ADGraphWrapper  aDGraphWrapper,ILogger<EmailProcessor> logger)
  {
    _appDbContext = appDbContext;
    _logger = logger;
    _aDGraphWrapper= aDGraphWrapper;
    _configurationSection = config.GetSection("URLs");
  }

  public void HandleAdminConsentConflict(Guid brokerId, string email)
  {
    throw new NotImplementedException();
  }
  /// <summary>
  /// for use with first sync where connectedEmail ID is unknown
  /// </summary>
  /// <param name="brokerId"></param>
  /// <param name="EmailNumber"></param>
  /// <param name="tenantId"></param>
  public void RenewSubscription(string email)
  {
    var connEmail = _appDbContext.ConnectedEmails.FirstOrDefault(x => x.Email == email);
    DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);

    var subs = new Subscription
    {
      ExpirationDateTime = SubsEnds
    };

    _aDGraphWrapper.CreateClient(connEmail.tenantId);
    var UpdatedSubs = _aDGraphWrapper._graphClient
      .Subscriptions[connEmail.GraphSubscriptionId.ToString()]
      .Request()
      .UpdateAsync(subs)
      .Result;

    connEmail.SubsExpiryDate = SubsEnds;
    var nextRenewalDate = SubsEnds - TimeSpan.FromMinutes(60);
    string RenewalJobId = BackgroundJob.Schedule<EmailProcessor>(s => s.RenewSubscription(connEmail.Email), nextRenewalDate);
    connEmail.SubsRenewalJobId = RenewalJobId;
    _appDbContext.SaveChanges();
  }


  public async Task CreateEmailSubscriptionAsync(ConnectedEmail connectedEmail, bool save = true)
  {
    var currDateTime = DateTime.UtcNow;
    //the maxinum subs period = just under 3 days
    DateTimeOffset SubsEnds = currDateTime + new TimeSpan(0, 4230, 0);

    var subs = new Subscription
    {
      ChangeType = "created",
      ClientState = VariousCons.MSFtWebhookSecret,
      ExpirationDateTime = SubsEnds,
      NotificationUrl = _configurationSection["MainAPI"] + "/MsftWebhook/Webhook",
      Resource = $"users/{connectedEmail.Email}/messages"
    };

    _aDGraphWrapper.CreateClient(connectedEmail.tenantId);
    //will validate through the webhook before returning the subscription here
    var CreatedSubs = await _aDGraphWrapper._graphClient.Subscriptions.Request().AddAsync(subs);

    //TODO run the analyzer to sync? see how the notifs creator and email analyzer will work
    //will have to consider current leads in the system, current listings assigned, websites from which
    //emails will be parsed to detect new leads

    connectedEmail.FirstSync = currDateTime;
    connectedEmail.LastSync = currDateTime;
    connectedEmail.SubsExpiryDate = (DateTime)(CreatedSubs.ExpirationDateTime?.UtcDateTime);
    connectedEmail.GraphSubscriptionId = Guid.Parse(CreatedSubs.Id);

    //renew 60 minutes before subs Ends
    var renewalTime = SubsEnds - TimeSpan.FromMinutes(60);
    string RenewalJobId = BackgroundJob.Schedule<EmailProcessor>(s => s.RenewSubscription(connectedEmail.Email), renewalTime);
    connectedEmail.SubsRenewalJobId = RenewalJobId;

    if (save) await _appDbContext.SaveChangesAsync();
  }

  public async Task CheckEmailSyncAsync(Guid SubsId, string tenantId)
  {
    //ADDCACHE
    var connEmail = await _appDbContext.ConnectedEmails.FirstAsync(e => e.GraphSubscriptionId == SubsId);
    if (!connEmail.SyncScheduled)
    {
      var jobId = Hangfire.BackgroundJob.Schedule<EmailProcessor>(e => e.SyncEmailAsync(connEmail.Email), TimeSpan.FromSeconds(15));
      connEmail.SyncScheduled = true;
      connEmail.SyncJobId = jobId;
    }
  }
  /// <summary>
  /// fetch all emails from last sync date and process them
  /// if admin get all emails from known lead providers
  /// </summary>
  /// <param name="connEmailId"></param>
  /// <param name="tenantId"></param>
  public async void SyncEmailAsync(string email)
  {
    //TODO Cache
    var connEmail = _appDbContext.ConnectedEmails
      .Select(e => new { e.Email, e.GraphSubscriptionId, e.LastSync, e.tenantId, e.Broker.isAdmin })
      .First(x => x.Email == email);
    _aDGraphWrapper.CreateClient(connEmail.tenantId);

    DateTimeOffset lastSync;
    if (connEmail.LastSync == null) lastSync = DateTimeOffset.UtcNow;
    else lastSync = (DateTimeOffset)connEmail.LastSync;

    List<Option> options = new List<Option>();
    var date = lastSync.ToString("o");
    options.Add(new QueryOption("$select", "id,sender,subject,isRead,conversationId,conversationIndex,receivedDateTime"));
    options.Add(new QueryOption("$filter", $"receivedDateTime gt {date}"));
    options.Add(new QueryOption("$orderby", "receivedDateTime"));

    options.Add(new HeaderOption("Prefer", "IdType=ImmutableId"));

    var messages = await _aDGraphWrapper._graphClient
      .Users[connEmail.Email]
      .MailFolders["Inbox"]
      .Messages
      .Request(options)
      .GetAsync();

    ProcessMessages(messages);

    while (messages.NextPageRequest != null)
    {
      messages = messages.NextPageRequest
        .Header("Prefer", "IdType=ImmutableId")
        .GetAsync().Result;
      ProcessMessages(messages);
    }
  }


  public void ProcessMessages(IMailFolderMessagesCollectionPage messages)
  {
    foreach (var message in messages)
    {
      _logger.LogWarning($"message: {message}");

      //if admin, for emails from lead providers, extract text and send to ChatGPT to get info.
      // 
      //maybe mark as processed with extension property?
    }
  }
}
