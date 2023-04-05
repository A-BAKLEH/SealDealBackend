using Core.Constants;
using Core.Domain.BrokerAggregate.EmailConnection;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Hangfire;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Web.Constants;
using Web.HTTPClients;

namespace Web.Processing.EmailAutomation;

public class EmailProcessor
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<EmailProcessor> _logger;
    private ADGraphWrapper _aDGraphWrapper;
    private readonly IConfigurationSection _configurationSection;
    private readonly OpenAIGPT35Service _GPT35Service;
    public EmailProcessor(AppDbContext appDbContext, IConfiguration config, ADGraphWrapper aDGraphWrapper, OpenAIGPT35Service openAIGPT35Service, ILogger<EmailProcessor> logger)
    {
        _appDbContext = appDbContext;
        _logger = logger;
        _aDGraphWrapper = aDGraphWrapper;
        _GPT35Service = openAIGPT35Service;
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

    public async Task CreateOutlookEmailCategoriesAsync(ConnectedEmail connectedEmail)
    {
        _aDGraphWrapper.CreateClient(connectedEmail.tenantId);
        var categories = await _aDGraphWrapper._graphClient.Users[connectedEmail.Email].Outlook.MasterCategories.Request().GetAsync();
        var cats = new List<string>() { APIConstants.SealDealProcessed, APIConstants.SeenOnSealDeal,APIConstants.SentBySealDeal};
        foreach (var cat in cats)
        {
            if (!categories.Any(x => x.DisplayName == cat))
            {
                var newCat = new OutlookCategory
                {
                    DisplayName = cat,
                };
                if(cat == APIConstants.SealDealProcessed)
                {
                    newCat.Color = CategoryColor.Preset0;
                }
                else if(cat == APIConstants.SeenOnSealDeal)
                {
                    newCat.Color = CategoryColor.Preset4;
                }
                else
                {
                    newCat.Color = CategoryColor.Preset7;
                }
                await _aDGraphWrapper._graphClient.Users[connectedEmail.Email].Outlook.MasterCategories.Request().AddAsync(newCat);
            }
        }
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
    /// </summary>
    /// <param name="connEmailId"></param>
    /// <param name="tenantId"></param>
    public async void SyncEmailAsync(string email)
    {
        //TODO Cache
        var connEmail = _appDbContext.ConnectedEmails
          .Select(e => new { e.Email, e.GraphSubscriptionId, e.LastSync, e.tenantId, e.Broker.isAdmin, e.Broker.isSolo, e.BrokerId })
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
        options.Add(new HeaderOption("Prefer", "odata.maxpagesize=15"));

        var messages = await _aDGraphWrapper._graphClient
          .Users[connEmail.Email]
          .MailFolders["Inbox"]
          .Messages
          .Request(options)
          .GetAsync();

        await ProcessMessagesAsync(messages, connEmail.isAdmin, connEmail.BrokerId, connEmail.isSolo);

        while (messages.NextPageRequest != null)
        {
            messages = messages.NextPageRequest
              .Header("Prefer", "IdType=ImmutableId")
              .GetAsync().Result;
            await ProcessMessagesAsync(messages, connEmail.isAdmin, connEmail.BrokerId, connEmail.isSolo);
        }
    }


    public async Task ProcessMessagesAsync(IMailFolderMessagesCollectionPage messages, bool isAdmin, Guid brokerId, bool SoloBroker)
    {
        var groupedMessagesBySender = messages.GroupBy(m => m.Sender.EmailAddress.Address);

        List<Task<OpenAIResponse?>> LeadProviderTasks = new();
        var GroupedleadProviderEmails = groupedMessagesBySender.Where(g => GlobalControl.LeadProviderEmails.Contains(g.Key));
        foreach (var emailsGrouping in GroupedleadProviderEmails)
        {
            string fromEmailAddress = emailsGrouping.Key;
            foreach (var email in emailsGrouping)
            {
                LeadProviderTasks.Add(_GPT35Service.ParseEmailAsync(email,true));
            }
        }

        List<Task<OpenAIResponse?>> UnknownSenderTasks = new();
        foreach (var messageGrp in groupedMessagesBySender)
        {
            string fromEmailAddress = messageGrp.Key;
            if (GlobalControl.LeadProviderEmails.Contains(fromEmailAddress)) continue;
            var lead = _appDbContext.Leads
                .Select(l => new { l.Id,l.Email,l.BrokerId})
                .FirstOrDefaultAsync(l => l.Email == fromEmailAddress && l.BrokerId == brokerId);
            if (lead != null)
            {
                foreach (var email in messageGrp)
                {
                    //TODO deal with correspondences from knwon leads
                }
            }
            else // email is from unknown, send to chat gpt
            {
                foreach (var email in messageGrp)
                {
                    //TODO deal with correspondences from unknwon senders
                    UnknownSenderTasks.Add(_GPT35Service.ParseEmailAsync(email, false));
                }
            }
        }
        //foreach (var messageGrp in groupedMessages)
        //{
        //    string fromEmailAddress = messageGrp.Key;

        //    //TODO implement list of known lead providers whose emails can be parsed
        //    //TODO index on listing street address maybe separate the fields of address
        //    bool processed = false;
        //    if (GlobalControl.LeadProviderEmails.Contains(fromEmailAddress))
        //    {
        //        //parse leads and addresses from all emails in messGrp

        //        //_GPT35Service.ParseEmailAsync();

        //        if(SoloBroker)
        //        {
        //        }
        //        foreach (var mess in messageGrp)
        //        {
        //            _logger.LogWarning($"admin: LeadParsing message content:\n{mess.Body.Content} \n");
        //            //determine preliminarly if actual new lead email by subject or style or whatever
        //            //if YES send to ChatGPT to extract fields including for listing address and lead info,
        //            //if NO log warning or error with
        //            //the email content so that it can be manually parsed and added to the system, skip this message

        //            //if lead extracted create lead, If NOT log parsed info and email text as error to manually review and
        //            //skip this message

        //            //search for listing record by street address AND agencyId of this broker
        //            //if found:
        //            //increment listing leads count
        //            //set lead listing to this listing
        //            //if soloBroker OR listing has only 1 assigned broker assign lead to this broker and create proper notifs
        //            //else lead is unassigned and create notifs for admin (same as the one created when creating lead manually
        //            //without assigning it


        //            //else lead does not have a listing

        //            //admin's notif for lead creation has emailId in props

        //            //var lead = CreateLead();

        //        }
        //        processed = true;
        //    }
        //    else//email sender is a known lead
        //    {
        //        //TODO cache
        //        var lead = await _appDbContext.Leads
        //          .Select(l => new { l.Id, l.Email, l.BrokerId })
        //          .FirstAsync(l => l.Email == fromEmailAddress && l.BrokerId == brokerId);
        //        if (lead != null)
        //        {
        //            foreach (var mess in messageGrp)
        //            {
        //                _logger.LogWarning($"Lead Interaction message content:\n{mess.Body.Content} \n");
        //                //TODO add email to notifs
        //            }
        //        }
        //        processed = true;
        //    }
        //    if (!processed)
        //    {
        //        //ask chat GPT if this email is from a potential client who is inquiring about something. and if yes
        //        //extract as much info as possible from email, create lead if possible. add notif with email ID and
        //        //new notif type "potential lead" or something
        //    }

        //    //TODO process all notifs created in this webhook en masse, maybe by using WaitingInBath processing status
        //    //and scheduling a task that will process all those notifs with that processing status for this
        //    //particular broker

        //    //TODO maybe mark as processed with extention property?

        //    //TODO increment GPT usage for stats if necessary
        //}
    }

    public Lead CreateLead(string email, int agencyId, string emailId, Guid brokerId, LeadType leadType, string? firstName, string? lastName, string? phoneNumber, int? listingId)
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
        lead.LeadHistoryEvents = new() { notifCreation };
        return lead;
    }  
}
