using System.Linq;
using Azure.Core;
using Core.Constants;
using Core.Domain.BrokerAggregate.EmailConnection;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Hangfire;
using Humanizer;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using Web.Constants;

namespace Web.Processing.EmailAutomation;

public class EmailProcessor
{
  private readonly AppDbContext _appDbContext;
  private readonly ILogger<EmailProcessor> _logger;
  private ADGraphWrapper _aDGraphWrapper;
  private readonly IConfigurationSection _configurationSection;
  public EmailProcessor(AppDbContext appDbContext, IConfiguration config, ADGraphWrapper aDGraphWrapper, ILogger<EmailProcessor> logger)
  {
    _appDbContext = appDbContext;
    _logger = logger;
    _aDGraphWrapper = aDGraphWrapper;
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
      var jobId = BackgroundJob.Schedule<EmailProcessor>(e => e.SyncEmailAsync(connEmail.Email), TimeSpan.FromSeconds(15));
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
      .Select(e => new { e.Email, e.GraphSubscriptionId, e.LastSync, e.tenantId, e.Broker.isAdmin, e.BrokerId })
      .First(x => x.Email == email);
    _aDGraphWrapper.CreateClient(connEmail.tenantId);

    DateTimeOffset lastSync;
    if (connEmail.LastSync == null) lastSync = DateTimeOffset.UtcNow;
    else lastSync = (DateTimeOffset)connEmail.LastSync;

    List<Option> options = new List<Option>();
    var date = lastSync.ToString("o");
    options.Add(new QueryOption("$select", "id,sender,subject,isRead,conversationId,conversationIndex,receivedDateTime,body"));
    options.Add(new QueryOption("$filter", $"receivedDateTime gt {date}"));
    options.Add(new QueryOption("$orderby", "receivedDateTime"));
    options.Add(new HeaderOption("Prefer", "IdType=ImmutableId"));

    var messages = await _aDGraphWrapper._graphClient
      .Users[connEmail.Email]
      .MailFolders["Inbox"]
      .Messages
      .Request(options)
      .GetAsync();

    await ProcessMessagesAsync(messages, connEmail.isAdmin, connEmail.BrokerId);

    while (messages.NextPageRequest != null)
    {
      messages = messages.NextPageRequest
        .Header("Prefer", "IdType=ImmutableId")
        .GetAsync().Result;
      await ProcessMessagesAsync(messages, connEmail.isAdmin, connEmail.BrokerId);
    }
  }


  public async Task ProcessMessagesAsync(IMailFolderMessagesCollectionPage messages, bool isAdmin, Guid brokerId)
  {
    var groupedMessages = messages.GroupBy(m => m.Sender.EmailAddress.Address);
    foreach (var messageGrp in groupedMessages)
    {
      string fromEmailAddress = messageGrp.Key;

      //TODO decide what happens if this lead does does belong to any broker yet but it is created as
      //agency lead
      //TODO decide if lead assignation and to who, for now lead is just created without being assigned
      //TODO implement list of knowns lead providers whose emails can be parsed
      //create lead from parsed email
      bool processed = false;
      if (isAdmin && GlobalControl.LeadProviderEmails.Contains(fromEmailAddress))
      {
        foreach (var mess in messageGrp)
        {
          _logger.LogWarning($"admin: LeadParsing message content:\n{mess.Body.Content} \n");
          //determine if actual new lead email
          //send to ChatGPT to extract fields
          //create lead in agency without assigning and send notif to admin.
          //var lead = CreateLead();
        }
        processed = true;
      }
      else
      {
        //TODO cache
        var lead = await _appDbContext.Leads
          .Select(l => new { l.Id, l.Email, l.BrokerId })
          .FirstAsync(l => l.BrokerId == brokerId && l.Email == fromEmailAddress);
        if(lead != null)
        {
          foreach (var mess in messageGrp)
          {
            _logger.LogWarning($"Lead Interaction message content:\n{mess.Body.Content} \n");

          }
        }
        processed = true;
      }
      if(!processed)
      {
        //maybe later run analyzer on emails that you are not parsing now
      }
      //TODO increase listing brought x leads count
      //TODO process all notifs created in this webhook en masse, maybe by using WaitingInBath processing status
      //and scheduling a task that will process all those notifs with that processing status for this
      //particular broker
      //TODO maybe mark as processed with extention property?
    }
  }

  public Lead CreateLead(string email,int agencyId,string emailId,Guid brokerId, LeadType leadType,string? firstName,string? lastName, string? phoneNumber,int? listingId  )
  {
    var lead = new Lead
    {
      AgencyId = agencyId,
      BrokerId = null,
      Email = email,
      LeadFirstName = firstName ?? "-",
      LeadLastName = lastName,
      PhoneNumber = phoneNumber,
      EntryDate = DateTime.UtcNow,
      leadType = leadType,
      source = LeadSource.emailAuto,
      LeadStatus = LeadStatus.New,
      ListingId = listingId,
    };
    lead.SourceDetails[NotificationJSONKeys.EmailId] = emailId;
    
    Notification notifCreation = new Notification
    {
      EventTimeStamp = DateTime.UtcNow,
      DeleteAfterProcessing = false,
      ProcessingStatus = ProcessingStatus.WaitingInBatch,
      NotifyBroker = true,
      ReadByBroker = false,
      BrokerId = brokerId,
      NotifType = NotifType.LeadCreated,
    };
    lead.LeadHistoryEvents = new() { notifCreation};
    return lead;
  }
}
