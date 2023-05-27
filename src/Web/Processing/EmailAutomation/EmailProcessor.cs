using Core.Constants;
using Core.Domain.AgencyAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.BrokerAggregate.EmailConnection;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.DTOs.ProcessingDTOs;
using Hangfire;
using Hangfire.Server;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Web.Constants;
using Web.ControllerServices.StaticMethods;
using Web.HTTPClients;
using Web.RealTimeNotifs;
using EventType = Core.Domain.NotificationAggregate.EventType;

namespace Web.Processing.EmailAutomation;

public class EmailProcessor
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<EmailProcessor> _logger;
    private ADGraphWrapper _aDGraphWrapper;
    private readonly IConfigurationSection _configurationSection;
    private readonly OpenAIGPT35Service _GPT35Service;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
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

        //connectedEmail.FirstSync = currDateTime;
        //connectedEmail.LastSync = currDateTime;
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
        if (StaticEmailConcurrencyHandler.EmailParsingdict.TryAdd(SubsId, true))
        {
            var connEmail = await _appDbContext.ConnectedEmails.FirstAsync(e => e.GraphSubscriptionId == SubsId);
            //'lock' obtained by putting subsID as key in dictionary
            string jobId = "";
            try
            {
                jobId = BackgroundJob.Schedule<EmailProcessor>(e => e.SyncEmailAsync(connEmail.Email, null), TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                _logger.LogError("{place} error scheduling email parsing with error {error}", "ScheduleEmailParseing", ex.Message);
                StaticEmailConcurrencyHandler.EmailParsingdict.TryRemove(SubsId, out var s);
                return;
            }
            try
            {
                connEmail.SyncScheduled = true;
                connEmail.SyncJobId = jobId;
                await _appDbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("{place} error saving db after scheduling email parsing with error {error}", "ScheduleEmailParseing", ex.Message);

                BackgroundJob.Delete(jobId);
                StaticEmailConcurrencyHandler.EmailParsingdict.TryRemove(SubsId, out var s);
            }
        }
    }

    public async Task TagFailedMessages(List<Message> failedMessages, string brokerEmail)
    {
        try
        {

            foreach (var mess in failedMessages)
            {
                await _aDGraphWrapper._graphClient.Users[brokerEmail]
                .Messages[mess.Id]
                .PatchAsync(new Message
                {
                    SingleValueExtendedProperties = new()
                                {
                                  new SingleValueLegacyExtendedProperty
                                  {
                                    Id = APIConstants.ReprocessMessExtendedPropId,
                                    Value = "1"
                                  }
                                }
                });
            }
        }
        catch (ODataError er)
        {
            _logger.LogError("{place} failed with error code {code} and error message {message}", "TagFailedMessages", er.Error.Code, er.Error.Message);
        }
    }

    /// <summary>
    /// failed messages since up to a week ago
    /// </summary>
    /// <param name = "failedMessages" ></ param >
    /// < returns ></ returns >
    public async Task<List<Message>> GetFailedMessages(string email)
    {
        try
        {
            var weekAgo1 = DateTimeOffset.UtcNow - TimeSpan.FromDays(7);
            var weekAgo = weekAgo1.ToString("o");
            var failedMessages1 = await _aDGraphWrapper._graphClient.Users[email]
            .MailFolders["Inbox"]
            .Messages
            .GetAsync(config =>
            {
                config.QueryParameters.Select = new string[] { "id", "sender", "from", "subject", "isRead", "conversationId", "receivedDateTime", "body" };
                config.QueryParameters.Filter = $"receivedDateTime gt {weekAgo} and singleValueExtendedProperties/any(ep:ep/id eq '{APIConstants.ReprocessMessExtendedPropId}' and ep/value eq '1')";
                config.QueryParameters.Orderby = new string[] { "receivedDateTime" };
                config.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"" });
            });
            return failedMessages1.Value;
        }
        catch (ODataError er)
        {
            _logger.LogError("{place} failed with error code {code} and error message {message}", "TagFailedMessages", er.Error.Code, er.Error.Message);
            return null;
        }
    }
    /// <summary>
    /// fetch all emails from last sync date and process them
    /// </summary>
    /// <param name="connEmailId"></param>
    /// <param name="tenantId"></param>
    public async void SyncEmailAsync(string email, PerformContext performContext)
    {
        //TODO Cache
        var connEmail = await _appDbContext.ConnectedEmails
          .Select(e => new { e.SyncJobId, e.Email, e.GraphSubscriptionId, e.LastSync, e.tenantId, e.AssignLeadsAuto, e.Broker.Language, e.OpenAITokensUsed, e.BrokerId, e.Broker.isAdmin, e.Broker.AgencyId, e.Broker.isSolo, e.Broker.FirstName, e.Broker.LastName })
          .FirstAsync(x => x.Email == email);
        _aDGraphWrapper.CreateClient(connEmail.tenantId);

        if (StaticEmailConcurrencyHandler.EmailParsingdict.TryGetValue((Guid)connEmail.GraphSubscriptionId, out var s))
        {
            if (performContext.BackgroundJob.Id != connEmail.SyncJobId)
            {
                _logger.LogCritical("{place} sync email job's connectedEmail syncJobId {dbSyncJobID} not equal to actual jobId {currentJobId}.", "syncEmail", connEmail.SyncJobId, performContext.BackgroundJob.Id);
                StaticEmailConcurrencyHandler.EmailParsingdict.TryRemove((Guid)connEmail.GraphSubscriptionId, out var ss);
                return;
            }
        }
        else
        {
            _logger.LogCritical("{place} sync email job started without SubsID {SubsId} in dictionary,returning.", "syncEmail", connEmail.GraphSubscriptionId);
            StaticEmailConcurrencyHandler.EmailParsingdict.TryRemove((Guid)connEmail.GraphSubscriptionId, out var ss);
            return;
        }

        var brokerDTO = new BrokerEmailProcessingDTO
        { Id = connEmail.BrokerId, brokerFirstName = connEmail.FirstName, brokerLastName = connEmail.LastName, AgencyId = connEmail.AgencyId, isAdmin = connEmail.isAdmin, isSolo = connEmail.isSolo, BrokerEmail = connEmail.Email, BrokerLanguge = connEmail.Language, AssignLeadsAuto = connEmail.AssignLeadsAuto };

        DateTimeOffset lastSync;
        if (connEmail.LastSync == null) lastSync = DateTimeOffset.UtcNow;
        else lastSync = (DateTimeOffset)connEmail.LastSync;

        //TODO deal with when lsatSync and FirstSync is null it means this is the first sync

        bool error = false;

        var date = lastSync.ToString("o");
        int totaltokens = 0;
        int pageSize = 15;

        DateTimeOffset LastProcessedTimestamp = lastSync;
        var ReprocessMessages = new List<Message>();

        bool ReachedFailedMessages = false;

        try
        {
            var messages = await _aDGraphWrapper._graphClient
          .Users[connEmail.Email]
          .MailFolders["Inbox"]
          .Messages
          .GetAsync(config =>
          {
              config.QueryParameters.Top = pageSize;
              config.QueryParameters.Select = new string[] { "id", "from", "subject", "isRead", "conversationId", "receivedDateTime", "body" };
              config.QueryParameters.Filter = $"receivedDateTime gt {date}";
              config.QueryParameters.Orderby = new string[] { "receivedDateTime" };
              config.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"", "outlook.body-content-type=\"text\"" });
          }
          );
            int count = 0;
            int pauseAfter = pageSize;
            List<Message> messagesList = new(pageSize);

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
                    },
                (req) =>
                {
                    // Re-add the header to subsequent requests
                    req.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"", "outlook.body-content-type=\"text\"" });
                    return req;
                }
                );
            await pageIterator.IterateAsync();

            while (pageIterator.State != PagingState.Complete)
            {
                //process the messages
                var toks1 = await ProcessMessagesAsync(messagesList, brokerDTO, ReprocessMessages);
                totaltokens += toks1;
                LastProcessedTimestamp = (DateTimeOffset)messagesList.Last().ReceivedDateTime;
                // Reset count and list
                count = 0;
                messagesList = new(pageSize);
                await pageIterator.ResumeAsync();
            }
            ReachedFailedMessages = true;
            //failed messages

            var failedMessages = await GetFailedMessages(email);
            if (failedMessages != null && failedMessages.Count > 0)
            {
                var faultedReprocess = new List<Message>();
                var toks = await ProcessMessagesAsync(failedMessages, brokerDTO, faultedReprocess);
                totaltokens += toks;
                var success = failedMessages.Where(m => !faultedReprocess.Contains(m));
                foreach (var markSuccessMessage in success)
                {
                    await _aDGraphWrapper._graphClient.Users[email]
                    .Messages[markSuccessMessage.Id]
                    .PatchAsync(new Message
                    {
                        SingleValueExtendedProperties = new()
                        {
                      new SingleValueLegacyExtendedProperty
                      {
                        Id = APIConstants.ReprocessMessExtendedPropId,
                        Value = "0"
                      }
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical("{place} sync email job failed ", "syncEmail");
            await _appDbContext.ConnectedEmails.Where(e => e.Email == email)
                .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.OpenAITokensUsed, e => e.OpenAITokensUsed + totaltokens)
                .SetProperty(e => e.SyncScheduled, false)
                .SetProperty(e => e.LastSync, LastProcessedTimestamp));
            await TagFailedMessages(ReprocessMessages, brokerDTO.BrokerEmail);
            StaticEmailConcurrencyHandler.EmailParsingdict.TryRemove((Guid)connEmail.GraphSubscriptionId, out var ss);
            return;
        }

        try
        {
            await _appDbContext.ConnectedEmails.Where(e => e.Email == email)
                .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.OpenAITokensUsed, e => e.OpenAITokensUsed + totaltokens)
                .SetProperty(e => e.SyncScheduled, false)
                .SetProperty(e => e.LastSync, LastProcessedTimestamp));
            await TagFailedMessages(ReprocessMessages, brokerDTO.BrokerEmail);
            StaticEmailConcurrencyHandler.EmailParsingdict.TryRemove((Guid)connEmail.GraphSubscriptionId, out var ss);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fields at end of email sync");
        }
    }

    /// <summary>
    /// needs brokerID
    /// </summary>
    /// <param name="message"></param>
    /// <param name="brokerEmail"></param>
    /// <returns></returns>
    public async Task<EmailEvent?> CreateEmailEventKnownLead(Message message, string brokerEmail, int leadId)
    {
        var date1 = DateTimeOffset.UtcNow - TimeSpan.FromDays(200);
        var date = date1.ToString("o");

        var messages = await _aDGraphWrapper._graphClient
          .Users[brokerEmail]
          .Messages
          .GetAsync(config =>
          {
              config.QueryParameters.Top = 3;
              config.QueryParameters.Select = new string[] { "id", "from", "conversationId", "receivedDateTime" };
              config.QueryParameters.Filter = $"receivedDateTime gt {date} and conversationId eq {message.ConversationId}";
              config.QueryParameters.Orderby = new string[] { "receivedDateTime" };
              config.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"" });
          }
          );
        var messList = messages.Value;
        var emailEvent = new EmailEvent
        {
            BrokerEmail = brokerEmail,
            Id = message.Id,
            LeadParsedFromEmail = false,
            Seen = (bool)message.IsRead,
            TimeReceived = (DateTimeOffset)message.ReceivedDateTime,
            LeadId = leadId
        };
        if (messList.Count == 1 && messList[0].Id == message.Id)
        {
            emailEvent.ConversationId = message.ConversationId;
            emailEvent.NeedsAction = true;
            emailEvent.RepliedTo = false;
        }
        else
        {
            if ((bool)message.IsRead) return null;
            emailEvent.NeedsAction = false;
        }
        return emailEvent;
    }
    /// <summary>
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="isAdmin"></param>
    /// <param name="brokerId"></param>
    /// <param name="SoloBroker"></param>
    /// <param name="brokerEmail"></param>
    /// <returns>number of tokens used</returns>
    public async Task<int> ProcessMessagesAsync(List<Message> messages, BrokerEmailProcessingDTO brokerDTO, List<Message> ReprocessMessages)
    {
        using var localdbContext = _contextFactory.CreateDbContext();
        int tokens = 0;
        var KnownLeadEmailEvents = new List<EmailEvent>();
        var KnownLeadTasks = new List<Tuple<Task<EmailEvent?>, Message>>();

        var groupedMessagesBySender = messages.GroupBy(m => m.From.EmailAddress.Address);

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
            var leadEmail = await localdbContext.LeadEmails
                .AsNoTracking()
                .FirstOrDefaultAsync(em => em.EmailAddress == fromEmailAddress && em.Lead.BrokerId == brokerDTO.Id);

            if (leadEmail != null)
            {
                var groupedByConvo = messageGrp.GroupBy(m => m.ConversationId);
                foreach (var convo in groupedByConvo)
                {
                    if (convo.Count() > 1) //multiple messages in a conversation, need reply false so only create emailEvents for unseen emails
                    {
                        KnownLeadEmailEvents.AddRange(convo.Where(m => !(bool)m.IsRead).Select(m => new EmailEvent
                        {
                            NeedsAction = false,
                            BrokerEmail = brokerDTO.BrokerEmail,
                            BrokerId = brokerDTO.Id,
                            Id = m.Id,
                            LeadId = leadEmail.LeadId,
                            LeadParsedFromEmail = false,
                            Seen = false,
                            TimeReceived = (DateTimeOffset)m.ReceivedDateTime
                        }));
                    }
                    else
                    { //just 1 message, check that its not in a conversation
                        KnownLeadTasks.Add(new Tuple<Task<EmailEvent?>, Message>(CreateEmailEventKnownLead(convo.First(), brokerDTO.BrokerEmail, leadEmail.LeadId), convo.First()));
                    }
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
        // ------------get known lead emails
        //TODO handle errors, especially concurrency / throttling issues with graph api
        try
        {
            await Task.WhenAll(KnownLeadTasks.Select(t => t.Item1));
        }
        catch { }

        var tempEvents = KnownLeadTasks.Select(t => t.Item1.Result).Where(e => e != null).ToList();
        tempEvents.ForEach(e => e.BrokerId = brokerDTO.Id);
        KnownLeadEmailEvents.AddRange(tempEvents);
        localdbContext.AddRange(KnownLeadEmailEvents);
        //--------- start chatGPT tasks
        try
        {
            await Task.WhenAll(LeadProviderTasks);
        }
        catch { }

        List<Tuple<Task<EmailparserDBRecrodsRes>, Message>> LeadProviderDBRecordsTasks = new(LeadProviderTasks.Count);
        for (int i = 0; i < LeadProviderTasks.Count; i++)
        {
            var leadTask = LeadProviderTasks[i];
            var message = LeadProviderTaskMessages[i];
            tokens += HandleTaskResult(leadTask, message, LeadProviderDBRecordsTasks, true, brokerDTO, ReprocessMessages);
        }
        try
        {
            await Task.WhenAll(UnknownSenderTasks);
        }
        catch { }

        List<Tuple<Task<EmailparserDBRecrodsRes>, Message>> UnknownDBRecordsTasks = new(UnknownSenderTasks.Count);
        for (int i = 0; i < UnknownSenderTasks.Count; i++)
        {
            var leadTask = UnknownSenderTasks[i];
            var message = UnknownSenderTaskMessages[i];
            tokens += HandleTaskResult(leadTask, message, UnknownDBRecordsTasks, false, brokerDTO, ReprocessMessages);
        }
        //--------------------

        //analyzing chatGPT results
        List<Tuple<EmailparserDBRecrodsRes, Message>> leadsAdded = new(LeadProviderDBRecordsTasks.Count + UnknownDBRecordsTasks.Count);
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
                localdbContext.Leads.Add(LeadProviderDBRecordsTask.Item1.Result.Lead);
                leadsAdded.Add(new Tuple<EmailparserDBRecrodsRes, Message>(LeadProviderDBRecordsTask.Item1.Result, LeadProviderDBRecordsTask.Item2));
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
                localdbContext.Leads.Add(UnknownDBRecordsTask.Item1.Result.Lead);
                leadsAdded.Add(new Tuple<EmailparserDBRecrodsRes, Message>(UnknownDBRecordsTask.Item1.Result, UnknownDBRecordsTask.Item2));
            }
        }

        //transaction-------------------------------
        using var transaction = await localdbContext.Database.BeginTransactionAsync();

        //TODO add events/emails that are not attached to lead to the dbcontext

        Dictionary<int, int> listingIdToNewLeadCount = new();
        leadsAdded.Where(l => l.Item1.Lead.ListingId != null).GroupBy(l => l.Item1.Lead.ListingId).ToList().ForEach(g => listingIdToNewLeadCount.Add((int)g.Key, g.Count()));

        // TODO later increment LeadsGeneratedCount maybe periodically in a task at the end of the day
        //await Task.WhenAll(listingIdToNewLeadCount.Select(async (kv) =>
        // {
        //     byte counter = 4;
        //     while (counter >= 0)
        //     {
        //         try
        //         {
        //             await localdbContext.Listings.Where(l => l.Id == kv.Key).ExecuteUpdateAsync(
        //                                    li => li.SetProperty(l => l.LeadsGeneratedCount, l => l.LeadsGeneratedCount + kv.Value));
        //             break;
        //         }
        //         catch { counter--; await Task.Delay((4 - counter + 1) * 200); }
        //     }
        // }));

        await localdbContext.SaveChangesAsync();


        //TODO test concurrency limit for graph api here
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
        //mark the messages that failed with tag ReprocessMessageId"
        await Task.WhenAll(ReprocessMessages.Select(async (message) =>
        {
            try
            {
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
        var appevents = leadsAdded.SelectMany(tup => tup.Item1.Lead.AppEvents).ToList();
        var emailevents = leadsAdded.SelectMany(tup => tup.Item1.Lead.EmailEvents).ToList();
        emailevents.AddRange(KnownLeadEmailEvents);
        await RealTimeNotifSender.SendRealTimeNotifsAsync(_logger, brokerDTO.Id, true, true, appevents, emailevents);
        return tokens;
    }
    /// <summary>
    /// called on gpt result of lead providers and unknown leads
    /// tries to fetch db listing, then creates new lead and notifs
    /// if new lead has listingId, then that was the unique listing found
    /// lead will contain appEvents and EmailEvents associated to him. for now all
    /// these app/email events are associated to lead directly no need to use appEvents and emailEvents fields.
    /// other appEvents are in the 'appevents' field of the response U HAVE TO ADD THEM TO DB CONTEXT.
    /// EmailEvents are in the 'emailevents'field of the response U HAVE TO ADD THEM TO DB CONTEXT.
    /// </summary>
    /// <param name="parsedContent"></param>
    /// <returns></returns>
    public async Task<EmailparserDBRecrodsRes> FetchListingAndCreateDBRecordsAsync(LeadParsingContent parsedContent, bool FromLeadProvider, BrokerEmailProcessingDTO brokerDTO, Message message)
    {
        using var context = _contextFactory.CreateDbContext();

        var result = new EmailparserDBRecrodsRes();
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

        //TODO automatic forwarding to assigned-to broker

        Language lang = brokerDTO.BrokerLanguge;
        if (parsedContent.Language != null) Enum.TryParse(parsedContent.Language, true, out lang);
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
            Language = lang,
        };
        lead.SourceDetails[NotificationJSONKeys.CreatedByFullName] = brokerDTO.brokerFirstName + " " + brokerDTO.brokerLastName;
        lead.SourceDetails[NotificationJSONKeys.CreatedById] = brokerDTO.Id.ToString();

        result.Lead = lead;

        AppEvent LeadCreationNotif = new()
        {
            EventTimeStamp = DateTime.UtcNow,
            DeleteAfterProcessing = false,
            ProcessingStatus = ProcessingStatus.NoNeed,
            NotifyBroker = true,
            ReadByBroker = false,
            BrokerId = brokerDTO.Id,
            EventType = EventType.LeadCreated,
        };
        LeadCreationNotif.Props[NotificationJSONKeys.EmailId] = message.Id;
        lead.AppEvents = new() { LeadCreationNotif };

        var emailEvent = new EmailEvent
        {
            Id = message.Id,
            BrokerId = brokerDTO.Id,
            LeadParsedFromEmail = true,
            BrokerEmail = brokerDTO.BrokerEmail,
            Seen = (bool)message.IsRead,
            TimeReceived = ((DateTimeOffset)message.ReceivedDateTime).UtcDateTime,
            NeedsAction = !FromLeadProvider, //method called when mess from leadProvider OR new lead 
            //directly messaging. When directly messaging it needs reply.
        };
        if (FromLeadProvider) emailEvent.LeadProviderEmail = message.From.EmailAddress.Address;
        else emailEvent.ConversationId = message.ConversationId;
        lead.EmailEvents = new() { emailEvent };

        if (brokerDTO.isSolo || !brokerDTO.isAdmin) //solo broker or non admin
        {
            //always assign lead to self, if no listing found THAT IS ASSIGNED
            //TO SELF for non-admin non-solo broker then no listing linked, if listing found for soloBroker always link
            LeadCreationNotif.EventType = EventType.LeadCreated | EventType.LeadAssignedToYou;
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
                        LeadCreationNotif.EventType = EventType.LeadCreated | EventType.LeadAssignedToYou;
                    }
                    //if admin email's auto lead assignment is turned on, assign to broker
                    else if (brokerDTO.AssignLeadsAuto)
                    {
                        lead.BrokerId = brokerToAssignToId;
                        LeadCreationNotif.EventType = EventType.LeadCreated | EventType.YouAssignedtoBroker;
                        LeadCreationNotif.Props[NotificationJSONKeys.AssignedToId] = brokerToAssignToId.ToString();
                        LeadCreationNotif.Props[NotificationJSONKeys.AssignedToFullName] = brokerToAssignToFullName;

                        AppEvent LeadAssignedNotif = new()
                        {
                            EventTimeStamp = DateTime.UtcNow,
                            DeleteAfterProcessing = false,
                            ProcessingStatus = ProcessingStatus.NoNeed,
                            NotifyBroker = true,
                            ReadByBroker = false,
                            BrokerId = brokerToAssignToId,
                            EventType = EventType.LeadAssignedToYou
                        };
                        LeadAssignedNotif.Props[NotificationJSONKeys.AssignedById] = brokerDTO.Id.ToString();
                        LeadAssignedNotif.Props[NotificationJSONKeys.AssignedByFullName] = $"{brokerDTO.brokerFirstName} {brokerDTO.brokerLastName}";

                        lead.AppEvents.Add(LeadAssignedNotif);
                    }
                    //else LeadCreationNotif will notify admin that unassigned lead is created
                    else
                    {
                        LeadCreationNotif.Props[NotificationJSONKeys.SuggestedAssignToId] = brokerToAssignToId.ToString();
                        LeadCreationNotif.Props[NotificationJSONKeys.suggestedAssignToFullName] = brokerToAssignToFullName;
                    }
                }
                else //0 or multiple brokers assigned to listing, dont assign lead to self (admin) or any other broker
                { }
            }
            else // admin => no listing found
            {
            }
        }
        return result;
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
    public int HandleTaskResult(Task<OpenAIResponse?> leadTask, Message message, List<Tuple<Task<EmailparserDBRecrodsRes>, Message>> DBRecordsTasks, bool FromLeadProvider, BrokerEmailProcessingDTO brokerDTO, List<Message> ReprocessMessages)
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
            DBRecordsTasks.Add(new Tuple<Task<EmailparserDBRecrodsRes>, Message>(FetchListingAndCreateDBRecordsAsync(result.content, FromLeadProvider, brokerDTO, message), message));
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
