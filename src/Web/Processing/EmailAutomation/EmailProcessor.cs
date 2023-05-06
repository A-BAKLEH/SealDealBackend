using Core.Constants;
using Core.Domain.AgencyAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.BrokerAggregate.EmailConnection;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.DTOs.ProcessingDTOs;
using Hangfire;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Web.Constants;
using Web.ControllerServices.StaticMethods;
using Web.HTTPClients;

namespace Web.Processing.EmailAutomation;

public class EmailProcessor
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<EmailProcessor> _logger;
    private ADGraphWrapper _aDGraphWrapper;
    private readonly IConfigurationSection _configurationSection;
    private readonly OpenAIGPT35Service _GPT35Service;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private List<Message> ReprocessMessages;
    public EmailProcessor(AppDbContext appDbContext, IConfiguration config,
        ADGraphWrapper aDGraphWrapper, OpenAIGPT35Service openAIGPT35Service, ILogger<EmailProcessor> logger, IDbContextFactory<AppDbContext> contextFactory)
    {
        _appDbContext = appDbContext;
        _logger = logger;
        _aDGraphWrapper = aDGraphWrapper;
        _GPT35Service = openAIGPT35Service;
        _configurationSection = config.GetSection("URLs");
        _contextFactory = contextFactory;
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
    public async void RenewSubscriptionAsync(string email)
    {
        var connEmail = _appDbContext.ConnectedEmails.FirstOrDefault(x => x.Email == email);
        DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);

        var subs = new Subscription
        {
            ExpirationDateTime = SubsEnds
        };

        _aDGraphWrapper.CreateClient(connEmail.tenantId);

        var UpdatedSubs = await _aDGraphWrapper._graphClient
          .Subscriptions[connEmail.GraphSubscriptionId.ToString()]
          .PatchAsync(subs);

        connEmail.SubsExpiryDate = SubsEnds;
        var nextRenewalDate = SubsEnds - TimeSpan.FromMinutes(60);
        string RenewalJobId = BackgroundJob.Schedule<EmailProcessor>(s => s.RenewSubscriptionAsync(connEmail.Email), nextRenewalDate);
        connEmail.SubsRenewalJobId = RenewalJobId;
        _appDbContext.SaveChanges();
    }

    public async Task CreateOutlookEmailCategoriesAsync(ConnectedEmail connectedEmail)
    {
        _aDGraphWrapper.CreateClient(connectedEmail.tenantId);
        var categs = await _aDGraphWrapper._graphClient.Users[connectedEmail.Email].Outlook.MasterCategories.GetAsync();
        List<OutlookCategory> categories = categs.Value;
        var cats = new List<string>() { APIConstants.NewLeadCreated, APIConstants.SeenOnSealDeal, APIConstants.SentBySealDeal };
        foreach (var cat in cats)
        {
            if (!categories.Any(x => x.DisplayName == cat))
            {
                var newCat = new OutlookCategory
                {
                    DisplayName = cat,
                };
                if (cat == APIConstants.NewLeadCreated)
                {
                    newCat.Color = CategoryColor.Preset0;
                }
                else if (cat == APIConstants.SeenOnSealDeal)
                {
                    newCat.Color = CategoryColor.Preset4;
                }
                else //SentBySealDeal
                {
                    newCat.Color = CategoryColor.Preset7;
                }
                await _aDGraphWrapper._graphClient.Users[connectedEmail.Email].Outlook.MasterCategories.PostAsync(newCat);
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
        var CreatedSubs = await _aDGraphWrapper._graphClient.Subscriptions.PostAsync(subs);

        //TODO run the analyzer to sync? see how the notifs creator and email analyzer will work
        //will have to consider current leads in the system, current listings assigned, websites from which
        //emails will be parsed to detect new leads

        connectedEmail.FirstSync = currDateTime;
        connectedEmail.LastSync = currDateTime;
        connectedEmail.SubsExpiryDate = (DateTime)(CreatedSubs.ExpirationDateTime?.UtcDateTime);
        connectedEmail.GraphSubscriptionId = Guid.Parse(CreatedSubs.Id);

        //renew 60 minutes before subs Ends
        var renewalTime = SubsEnds - TimeSpan.FromMinutes(60);
        string RenewalJobId = BackgroundJob.Schedule<EmailProcessor>(s => s.RenewSubscriptionAsync(connectedEmail.Email), renewalTime);
        connectedEmail.SubsRenewalJobId = RenewalJobId;

        if (save) await _appDbContext.SaveChangesAsync();
    }

    public async Task CheckEmailSyncAsync(Guid SubsId, string tenantId)
    {
        //ADDCACHE
        var connEmail = await _appDbContext.ConnectedEmails.FirstAsync(e => e.GraphSubscriptionId == SubsId);
        if (!connEmail.SyncScheduled)
        {
            var jobId = BackgroundJob.Schedule<EmailProcessor>(e => e.SyncEmailAsync(connEmail.Email), TimeSpan.FromSeconds(6));
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
        var connEmail = await _appDbContext.ConnectedEmails
          .Select(e => new { e.Email, e.GraphSubscriptionId, e.LastSync, e.tenantId, e.AssignLeadsAuto, e.Broker.Languge, e.OpenAITokensUsed, e.BrokerId, e.Broker.isAdmin, e.Broker.AgencyId, e.Broker.isSolo, e.Broker.FirstName, e.Broker.LastName })
          .FirstAsync(x => x.Email == email);
        _aDGraphWrapper.CreateClient(connEmail.tenantId);

        var brokerDTO = new BrokerEmailProcessingDTO
        { Id = connEmail.BrokerId, brokerFirstName = connEmail.FirstName, brokerLastName = connEmail.LastName, AgencyId = connEmail.AgencyId, isAdmin = connEmail.isAdmin, isSolo = connEmail.isSolo, BrokerEmail = connEmail.Email, BrokerLanguge = connEmail.Languge, AssignLeadsAuto = connEmail.AssignLeadsAuto };

        DateTimeOffset lastSync;
        if (connEmail.LastSync == null) lastSync = DateTimeOffset.UtcNow;
        else lastSync = (DateTimeOffset)connEmail.LastSync;

        //TODO fetch failed messages with extension property "reprocess", these were failed from previous runs
        var date = lastSync.ToString("o");
        int totaltokens = 0;
        var messages = await _aDGraphWrapper._graphClient
          .Users[connEmail.Email]
          .MailFolders["Inbox"]
          .Messages
          .GetAsync(config =>
              {
                  config.QueryParameters.Top = 15;
                  config.QueryParameters.Select = new string[] { "id", "sender", "subject", "isRead", "conversationId", "conversationIndex", "receivedDateTime", "body" };
                  config.QueryParameters.Filter = $"receivedDateTime gt {date}";
                  config.QueryParameters.Orderby = new string[] { "receivedDateTime" };
                  config.Headers.Add("Prefer", new string[] { "IdType=ImmutableId" });
              }
          );
        int count = 0;
        int pauseAfter = 15;
        List<Message> messagesList = new(15);

        var pageIterator = PageIterator<Message, MessageCollectionResponse>
            .CreatePageIterator(
            _aDGraphWrapper._graphClient,
            messages,
                (m) =>
                {
                    messagesList.Add(m);
                    count++;
                    // If we've iterated over the limit,
                    // stop the iteration by returning false
                    return count < pauseAfter;
                }
            );
        await pageIterator.IterateAsync();

        while (pageIterator.State != PagingState.Complete)
        {
            //process the messages
            var toks = await ProcessMessagesAsync(messagesList, brokerDTO);
            totaltokens += toks;
            // Reset count and list
            count = 0;
            messagesList = new(15);
            await pageIterator.ResumeAsync();
        }

        var connectedEmail = new ConnectedEmail { Email = connEmail.Email, OpenAITokensUsed = connEmail.OpenAITokensUsed + totaltokens };
        _appDbContext.ConnectedEmails.Attach(connectedEmail);

        await saveWConcurrencyHandling(totaltokens);
    }


    /// <summary>
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="isAdmin"></param>
    /// <param name="brokerId"></param>
    /// <param name="SoloBroker"></param>
    /// <param name="brokerEmail"></param>
    /// <returns>number of tokens used</returns>
    public async Task<int> ProcessMessagesAsync(List<Message> messages, BrokerEmailProcessingDTO brokerDTO)
    {
        using var localdbContext = _contextFactory.CreateDbContext();
        int tokens = 0;

        var groupedMessagesBySender = messages.GroupBy(m => m.Sender.EmailAddress.Address);
        ReprocessMessages = new List<Message>();

        var GroupedleadProviderEmails = groupedMessagesBySender.Where(g => GlobalControl.LeadProviderEmails.Contains(g.Key));
        List<Task<OpenAIResponse?>> LeadProviderTasks = new();
        List<Message> LeadProviderTaskMessages = new();
        foreach (var emailsGrouping in GroupedleadProviderEmails)
        {
            string fromEmailAddress = emailsGrouping.Key;
            foreach (var email in emailsGrouping)
            {
                LeadProviderTasks.Add(_GPT35Service.ParseEmailAsync(email, true));
                LeadProviderTaskMessages.Add(email);
            }
        }

        List<Task<OpenAIResponse?>> UnknownSenderTasks = new();
        List<Message> UnknownSenderTaskMessages = new();
        foreach (var messageGrp in groupedMessagesBySender)
        {
            string fromEmailAddress = messageGrp.Key;
            if (GlobalControl.LeadProviderEmails.Contains(fromEmailAddress)) continue;

            //TODO cache this
            var lead = await localdbContext.LeadEmails
                .AsNoTracking()
                .FirstOrDefaultAsync(em => em.EmailAddress == fromEmailAddress && em.Lead.BrokerId == brokerDTO.Id);

            if (lead != null)
            {
                foreach (var email in messageGrp)
                {
                    //TODO deal with correspondences from known leads
                    //deal with it here since u dont need to wait on any external calls
                    //will depend on notifs system
                    //TODO remember that all these messages are from the same known lead
                }
            }
            else // email is from unknown, send to chat gpt
            {
                foreach (var email in messageGrp)
                {
                    //TODO take into consideration that this unknown sender might send multiple messages
                    UnknownSenderTasks.Add(_GPT35Service.ParseEmailAsync(email, false));
                    UnknownSenderTaskMessages.Add(email);
                }
            }
        }
        //--------- start chatGPT tasks
        try
        {
            await Task.WhenAll(LeadProviderTasks);
        }
        catch { }

        List<Tuple<Task<Lead>, Message>> LeadProviderDBRecordsTasks = new(LeadProviderTasks.Count);
        for (int i = 0; i < LeadProviderTasks.Count; i++)
        {
            var leadTask = LeadProviderTasks[i];
            var message = LeadProviderTaskMessages[i];
            tokens += HandleTaskResult(leadTask, message, LeadProviderDBRecordsTasks, true, brokerDTO);
        }
        try
        {
            await Task.WhenAll(UnknownSenderTasks);
        }
        catch { }

        List<Tuple<Task<Lead>, Message>> UnknownDBRecordsTasks = new(UnknownSenderTasks.Count);
        for (int i = 0; i < UnknownSenderTasks.Count; i++)
        {
            var leadTask = UnknownSenderTasks[i];
            var message = UnknownSenderTaskMessages[i];
            tokens += HandleTaskResult(leadTask, message, UnknownDBRecordsTasks, false, brokerDTO);
        }
        //--------------------

        //analyzing chatGPT results
        List<Tuple<Lead, Message>> leadsAdded = new(LeadProviderDBRecordsTasks.Count + UnknownDBRecordsTasks.Count);
        try
        {
            await Task.WhenAll(LeadProviderDBRecordsTasks.ConvertAll(x => x.Item1));
        }
        catch { }

        foreach (var LeadProviderDBRecordsTask in LeadProviderDBRecordsTasks)
        {
            if (LeadProviderDBRecordsTask.Item1.IsFaulted) //Task Error : this shouldnt happen as there is try catch block inside tasks
            {
                //TODO check error type to discard email if needed
                ReprocessMessages.Add(LeadProviderDBRecordsTask.Item2);
                //TODO change error message if email discarded
                _logger.LogError("{Category} dbRecordsCreation and error {Error}", "FetchListingAndCreateDBRecordsAsync", LeadProviderDBRecordsTask.Item1.Exception.Message);
            }
            else
            {
                localdbContext.Leads.Add(LeadProviderDBRecordsTask.Item1.Result);
                leadsAdded.Add(new Tuple<Lead, Message>(LeadProviderDBRecordsTask.Item1.Result, LeadProviderDBRecordsTask.Item2));
            }
        }

        try
        {
            await Task.WhenAll(UnknownDBRecordsTasks.ConvertAll(x => x.Item1));
        }
        catch { }

        foreach (var UnknownDBRecordsTask in UnknownDBRecordsTasks)
        {
            if (UnknownDBRecordsTask.Item1.IsFaulted) //Task Error : this shouldnt happen as there is try catch block inside tasks
            {
                //TODO check error type to discard email if needed
                ReprocessMessages.Add(UnknownDBRecordsTask.Item2);
                //TODO change error message if email discarded
                _logger.LogError("{Category} dbRecordsCreation and error {Error}", "FetchListingAndCreateDBRecordsAsync", UnknownDBRecordsTask.Item1.Exception.Message);
            }
            else
            {
                localdbContext.Leads.Add(UnknownDBRecordsTask.Item1.Result);
                leadsAdded.Add(new Tuple<Lead, Message>(UnknownDBRecordsTask.Item1.Result, UnknownDBRecordsTask.Item2));
            }
        }

        //transaction-------------------------------
        using var transaction = await localdbContext.Database.BeginTransactionAsync();

        Dictionary<int, int> listingIdToNewLeadCount = new();
        leadsAdded.Where(l => l.Item1.ListingId != null).GroupBy(l => l.Item1.ListingId).ToList().ForEach(g => listingIdToNewLeadCount.Add((int)g.Key, g.Count()));

        //increment LeadsGeneratedCount
        await Task.WhenAll(listingIdToNewLeadCount.Select(async (kv) =>
        {
            byte counter = 4;
            while (counter >= 0)
            {
                try
                {
                    await localdbContext.Listings.Where(l => l.Id == kv.Key).ExecuteUpdateAsync(
                                           li => li.SetProperty(l => l.LeadsGeneratedCount, l => l.LeadsGeneratedCount + kv.Value));
                    break;
                }
                catch { counter--; await Task.Delay((4 - counter + 1) * 200); }
            }
        }));

        await localdbContext.SaveChangesAsync();

        //TODO process all notifs created in this webhook en masse, maybe by using WaitingInBatch processing status
        //and scheduling a task that will process all those notifs with that processing status for this
        //particular broker
        //notifs : lead created, lead created | assigned, lead assigned, notifs for leads already known


        //mark the messages that had a lead with "LeadExtracted"
        await Task.WhenAll(leadsAdded.Select(async (tup) =>
        {
            try
            {
                tup.Item2.Categories.Add(APIConstants.NewLeadCreated);
                await _aDGraphWrapper._graphClient.Users[brokerDTO.BrokerEmail].Messages[tup.Item2.Id]
                .PatchAsync(tup.Item2);
            }
            catch (Exception ex)
            {
                _logger.LogError("{Category} patching email category after processing error: {Error}", "GraphSDK", ex.Message);
            }
        }));

        await Task.WhenAll(ReprocessMessages.Select(async (message) =>
        {
            try { 
                message.SingleValueExtendedProperties = new()
                {
                  new SingleValueLegacyExtendedProperty
                  {
                    Id = APIConstants.ReprocessMessExtendedPropId,
                    Value = "1"
                  }
                };
                await _aDGraphWrapper._graphClient.Users[brokerDTO.BrokerEmail].Messages[message.Id]
                .PatchAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError("{Category} assigning reprocess prop on emails after processing error: {Error}", "GraphSDK", ex.Message);
            }
        }));

        await transaction.CommitAsync();
        //transaction-------------------------------
        return tokens;
    }

    /// <summary>
    /// called on gpt result of lead providers and unknown leads
    /// tries to fetch db listing, then creates new lead and notifs
    /// if new lead has listingId, then that was the unique listing found
    /// </summary>
    /// <param name="parsedContent"></param>
    /// <returns></returns>
    public async Task<Lead> FetchListingAndCreateDBRecordsAsync(LeadParsingContent parsedContent, bool FromLeadProvider, BrokerEmailProcessingDTO brokerDTO, Message message)
    {
        using var context = _contextFactory.CreateDbContext();

        //TODO lead assignment rules need to be revised, with probably a setting on connectedEmails that tells if this email's leads
        //should be automatically assigned

        bool listingFound = false;
        bool multipleListingsMatch = false;
        (int ListingId, Address? address, string? gptAddress, List<BrokerListingAssignment>? brokersAssigned) MatchingListingTuple = (0, null, null, null);
        if (!string.IsNullOrEmpty(parsedContent.PropertyAddress) && !string.IsNullOrEmpty(parsedContent.StreetAddress))
        {
            //get listing
            var streetAddressFormatted = parsedContent.StreetAddress.FormatStreetAddress();
            var listings = await context.Listings
                .Where(x => x.AgencyId == brokerDTO.AgencyId && EF.Functions.Like(x.FormattedStreetAddress, $"{streetAddressFormatted}%"))
                .Select(x => new { x.Id, x.FormattedStreetAddress, x.Address, x.BrokersAssigned })
                .ToListAsync();
            //TODO if doesnt work try searching only with building number and then asking gpt like exlpained in 'complicated way'
            //in the text file, for now this is the simple way

            if (listings != null && listings.Count == 1) //1 listing matches, bingo!
            {
                MatchingListingTuple.ListingId = listings[0].Id;
                MatchingListingTuple.address = listings[0].Address;
                MatchingListingTuple.brokersAssigned = listings[0].BrokersAssigned;
                listingFound = true;
            }
            else if (listings.Count > 1) //more than 1 match
            {
                if (!string.IsNullOrEmpty(parsedContent.Apartment))
                {
                    var lis = listings.FirstOrDefault(a => a.Address.apt == parsedContent.Apartment);
                    if (lis != null)
                    {
                        MatchingListingTuple.ListingId = lis.Id;
                        MatchingListingTuple.address = lis.Address;
                        MatchingListingTuple.brokersAssigned = lis.BrokersAssigned;
                        listingFound = true;
                    }
                }
                if (!listingFound) multipleListingsMatch = true;
            }
        }
        //this is only for admin-tier accounts(including later secretaries,assistants,etc): when true, create lead
        //but ONLY assign to broker if he is the only one assigned to that listing AND in
        //connectedEmail/adminSettings automatic lead assignment is turned on
        //if false, create lead and never assign to anyone
        //TODO later determine if the original email should be forwarded to the broker or not based on sensitive info
        //also good to have a manual setting that admins can set

        Languge lang = Languge.English;
        Enum.TryParse(parsedContent.Language, true, out lang);
        var lead = new Lead
        {
            AgencyId = brokerDTO.AgencyId,
            LeadFirstName = parsedContent.firstName ?? "-",
            LeadLastName = parsedContent.lastName ?? "-",
            PhoneNumber = parsedContent.phoneNumber,
            EntryDate = DateTime.UtcNow,
            leadType = LeadType.Unknown,
            source = LeadSource.emailAuto,
            LeadStatus = LeadStatus.New,
            LeadEmails = new() { new LeadEmail { EmailAddress = message.Sender.EmailAddress.Address } },
            Languge = lang,
        };
        lead.SourceDetails[NotificationJSONKeys.EmailId] = message.Id;

        Notification LeadCreationNotif = new()
        {
            EventTimeStamp = DateTime.UtcNow,
            DeleteAfterProcessing = false,
            ProcessingStatus = ProcessingStatus.WaitingInBatch,
            NotifyBroker = true,
            ReadByBroker = false,
            BrokerId = brokerDTO.Id,
            NotifType = NotifType.LeadCreated,
        };
        lead.LeadHistoryEvents = new() { LeadCreationNotif };
        if (brokerDTO.isSolo || !brokerDTO.isAdmin) //solo broker or non admin
        {
            //always assign lead to self, if no listing found THAT IS ASSIGNED
            //TO SELF for non-admin non-solo broker then no listing linked, if listing found for soloBroker always link
            LeadCreationNotif.NotifType = NotifType.LeadCreated | NotifType.LeadAssigned;
            lead.BrokerId = brokerDTO.Id;
            if (listingFound)
            {
                if (brokerDTO.isSolo || (!brokerDTO.isAdmin && MatchingListingTuple.brokersAssigned.Any(x => x.BrokerId == brokerDTO.Id)))
                { lead.ListingId = MatchingListingTuple.ListingId; }
                else
                {
                    //non-solo non-admin broker, listing found but not assigned to him
                }
            }
            //not useful now
            else if (multipleListingsMatch)
            {
                // lead.SourceDetails[NotificationJSONKeys.MultipleMatchingListings] = "true";
            }
            else
            {
                //lead.SourceDetails[NotificationJSONKeys.NoMatchingListings] = "true";
            }
        }
        else//admin
        {
            //admin of an agency OR, admin in a small team where everyone is admin
            if (listingFound)
            {
                //assign to broker if 1 broker linked with listing AND autoAssign on OR self, else dont assign.
                lead.ListingId = MatchingListingTuple.ListingId;
                if (MatchingListingTuple.brokersAssigned != null && MatchingListingTuple.brokersAssigned.Count == 1)
                {
                    var brokerToAssignToId = MatchingListingTuple.brokersAssigned[0].BrokerId;
                    string brokerToAssignToFullName = "";
                    if (brokerToAssignToId != brokerDTO.Id)
                    {
                        var brokertoAssignTo = await context.Brokers.Select(b => new { b.Id, b.FirstName, b.LastName }).FirstOrDefaultAsync(b => b.Id == brokerToAssignToId);
                        brokerToAssignToFullName = $"{brokertoAssignTo.FirstName} {brokertoAssignTo.LastName}";
                    }

                    //if self, no extra notif needed
                    if (brokerToAssignToId == brokerDTO.Id)//if assigned to self
                    {
                        lead.BrokerId = brokerDTO.Id;
                        LeadCreationNotif.NotifType = NotifType.LeadCreated | NotifType.LeadAssigned;
                    }
                    //if admin email's auto lead assignment is turned on, assign to broker
                    else if (brokerDTO.AssignLeadsAuto)
                    {
                        lead.BrokerId = brokerToAssignToId;

                        LeadCreationNotif.NotifProps[NotificationJSONKeys.AssignedToId] = brokerToAssignToId.ToString();
                        LeadCreationNotif.NotifProps[NotificationJSONKeys.AssignedToFullName] = brokerToAssignToFullName;

                        Notification LeadAssignedNotif = new()
                        {
                            EventTimeStamp = DateTime.UtcNow,
                            DeleteAfterProcessing = false,
                            ProcessingStatus = ProcessingStatus.WaitingInBatch,
                            NotifyBroker = true,
                            ReadByBroker = false,
                            BrokerId = brokerToAssignToId,
                            NotifType = NotifType.LeadAssigned
                        };
                        LeadAssignedNotif.NotifProps[NotificationJSONKeys.AssignedById] = brokerDTO.Id.ToString();
                        LeadAssignedNotif.NotifProps[NotificationJSONKeys.AssignedByFullName] = $"{brokerDTO.brokerFirstName} {brokerDTO.brokerLastName}";

                        lead.LeadHistoryEvents.Add(LeadAssignedNotif);
                    }
                    //else LeadCreationNotif will notify admin that unassigned lead is created
                    else
                    {
                        LeadCreationNotif.NotifProps[NotificationJSONKeys.SuggestedAssignToId] = brokerToAssignToId.ToString();
                        LeadCreationNotif.NotifProps[NotificationJSONKeys.suggestedAssignToFullName] = brokerToAssignToFullName;
                    }
                }
                else //0 or multiple brokers assigned to listing, dont assign lead to self (admin) or any other broker
                { }
            }
            else // admin => no listing found
            {
            }
        }
        //TODO if listing found always increase count of leads brought by this listing

        return lead;
    }
    /// <summary>
    /// if task successful, runs task that will fetch listing and create db records to be inserted
    /// </summary>
    /// <param name="leadTask"></param>
    /// <param name="message"></param>
    /// <param name="DBRecordsTasks"></param>
    /// <param name="FromLeadProvider"></param>
    /// <param name="SoloBroker"></param>
    /// <param name="isAdmin"></param>
    /// <param name="brokerId"></param>
    /// <returns></returns>
    public int HandleTaskResult(Task<OpenAIResponse?> leadTask, Message message, List<Tuple<Task<Lead>, Message>> DBRecordsTasks, bool FromLeadProvider, BrokerEmailProcessingDTO brokerDTO)
    {
        if (leadTask.IsFaulted) //Task Error : this shouldnt happen as there is try catch block inside tasks
        {
            //TODO check error type to discard email if needed
            ReprocessMessages.Add(message);
            //TODO change error message if email discarded
            _logger.LogError("{Category} GPT 3.5 email parsing and error {Error}", "OpenAI", leadTask.Exception.Message);
            return 0;
        }

        var result = leadTask.Result;
        if (!result.Success)
        {
            //TODO check error type to discard email if needed
            ReprocessMessages.Add(message);
            //TODO change error message if email discarded
            _logger.LogError("{Category} GPT 3.5 email parsing and error {Error}", "OpenAI", result.ErrorType.ToString() + ": " + result.ErrorMessage);
        }
        else if (result.HasLead) //no error and has lead
        {
            DBRecordsTasks.Add(new Tuple<Task<Lead>, Message>(FetchListingAndCreateDBRecordsAsync(result.content, FromLeadProvider, brokerDTO, message), message));
        }
        else
        {
            //discard email, no lead found
        }
        return result.EmailTokensUsed;
    }

    private async Task saveWConcurrencyHandling(int newTokens)
    {
        bool saved = false;
        while (!saved)
        {
            try
            {
                await _appDbContext.SaveChangesAsync();
                saved = true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    if (entry.Entity is ConnectedEmail)
                    {
                        var proposedValues = entry.CurrentValues;
                        var databaseValues = entry.GetDatabaseValues();

                        databaseValues.TryGetValue("OpenAITokensUsed", out int dbcount);
                        dbcount += newTokens;
                        ConnectedEmail connEmail = (ConnectedEmail)entry.Entity;
                        connEmail.OpenAITokensUsed = dbcount;
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
