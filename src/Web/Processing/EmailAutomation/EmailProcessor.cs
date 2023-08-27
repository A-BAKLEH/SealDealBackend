using Core.Config.Constants.LoggingConstants;
using Core.Constants;
using Core.Domain.ActionPlanAggregate;
using Core.Domain.AgencyAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.BrokerAggregate.EmailConnection;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.DTOs.ProcessingDTOs;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Hangfire;
using Hangfire.Server;
using HtmlAgilityPack;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;
using MimeKit;
using Newtonsoft.Json.Linq;
using Serilog.Context;
using System;
using System.Net.Mail;
using System.Text;
using Web.Constants;
using Web.ControllerServices.StaticMethods;
using Web.HTTPClients;
using Web.Processing.ActionPlans;
using Web.RealTimeNotifs;
using EventType = Core.Domain.NotificationAggregate.EventType;
using GmailMessage = Google.Apis.Gmail.v1.Data.Message;
using MsftMessage = Microsoft.Graph.Models.Message;

namespace Web.Processing.EmailAutomation;

public class EmailProcessor
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<EmailProcessor> _logger;
    private ADGraphWrapper _aDGraphWrapper;
    private readonly IConfigurationSection _configurationSection;
    private readonly OpenAIGPT35Service _GPT35Service;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly RealTimeNotifSender _realTimeNotif;
    private GmailService? _GmailService;
    private readonly IConfigurationSection _GmailSection;
    private readonly IWebHostEnvironment _webHostEnv;
    public EmailProcessor(AppDbContext appDbContext, IConfiguration config,
        ADGraphWrapper aDGraphWrapper, OpenAIGPT35Service openAIGPT35Service,
        IWebHostEnvironment env,
        RealTimeNotifSender realTimeNotif, ILogger<EmailProcessor> logger, IDbContextFactory<AppDbContext> contextFactory)
    {
        _appDbContext = appDbContext;
        _logger = logger;
        _aDGraphWrapper = aDGraphWrapper;
        _GPT35Service = openAIGPT35Service;
        _configurationSection = config.GetSection("URLs");
        _GmailSection = config.GetSection("Gmail");
        _contextFactory = contextFactory;
        _realTimeNotif = realTimeNotif;
        _webHostEnv = env;
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
    public async Task RenewSubscriptionAsync(string email, CancellationToken cancellationToken)
    {
        var connEmail = await _appDbContext.ConnectedEmails.FirstOrDefaultAsync(x => x.Email == email);
        DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);

        var subs = new Subscription
        {
            ExpirationDateTime = SubsEnds
        };

        _aDGraphWrapper.CreateClient(connEmail.tenantId);

        try
        {
            var UpdatedSubs = await _aDGraphWrapper._graphClient
                .Subscriptions[connEmail.GraphSubscriptionId.ToString()]
                .PatchAsync(subs);
        }
        catch (ODataError err)
        {
            if (err.ResponseStatusCode == 404)
            {
                await CreateEmailSubscriptionAsync(connEmail, true);
                return;
            }
        }

        connEmail.SubsExpiryDate = SubsEnds.UtcDateTime;
        var nextRenewalDate = SubsEnds - TimeSpan.FromMinutes(120);
        string RenewalJobId = BackgroundJob.Schedule<EmailProcessor>(s => s.RenewSubscriptionAsync(connEmail.Email, CancellationToken.None), nextRenewalDate);
        connEmail.SubsRenewalJobId = RenewalJobId;
        _appDbContext.SaveChanges();
    }

    public async Task CreateOutlookEmailCategoriesAsync(ConnectedEmail connectedEmail)
    {
        _aDGraphWrapper.CreateClient(connectedEmail.tenantId);
        var categs = await _aDGraphWrapper._graphClient.Users[connectedEmail.Email].Outlook.MasterCategories.GetAsync();
        List<OutlookCategory> categories = categs.Value;
        var cats = new List<string>() { APIConstants.NewLeadCreated, APIConstants.SeenOnSealDeal,
            //APIConstants.VerifyEmailAddress,
            APIConstants.SentBySealDeal };
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
                //else if (cat == APIConstants.VerifyEmailAddress)
                //{
                //    newCat.Color = CategoryColor.Preset3;
                //}
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
            Resource = $"users/{connectedEmail.Email}/mailFolders/inbox/messages"
        };

        _aDGraphWrapper.CreateClient(connectedEmail.tenantId);
        //will validate through the webhook before returning the subscription here
        Subscription? CreatedSubs;
        byte count = 0;
    Loop1:
        try
        {
            count++;
            CreatedSubs = await _aDGraphWrapper._graphClient.Subscriptions.PostAsync(subs);
        }
        catch (ODataError ex)
        {
            if (ex.ResponseStatusCode == 403 && count <= 3)
            {
                await Task.Delay(2000);
                SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);
                subs = new Subscription
                {
                    ChangeType = "created",
                    ClientState = VariousCons.MSFtWebhookSecret,
                    ExpirationDateTime = SubsEnds,
                    NotificationUrl = _configurationSection["MainAPI"] + "/MsftWebhook/Webhook",
                    Resource = $"users/{connectedEmail.Email}/mailFolders/inbox/messages"
                };
                goto Loop1;
            }
            else
            {
                _logger.LogError("{tag} errorMessage {errorMessage} with counter {counter}", "handleAdminConsent", ex.Error.Message, count);
                throw;
            }

        }
        //TODO run the analyzer to sync? see how the notifs creator and email analyzer will work
        //will have to consider current leads in the system, current listings assigned, websites from which
        //emails will be parsed to detect new leads

        //connectedEmail.FirstSync = currDateTime;
        //connectedEmail.LastSync = currDateTime;
        connectedEmail.SubsExpiryDate = (DateTime)(CreatedSubs.ExpirationDateTime?.UtcDateTime);
        connectedEmail.GraphSubscriptionId = Guid.Parse(CreatedSubs.Id);

        //renew 60 minutes before subs Ends
        var renewalTime = SubsEnds - TimeSpan.FromMinutes(120);
        string RenewalJobId = BackgroundJob.Schedule<EmailProcessor>(s => s.RenewSubscriptionAsync(connectedEmail.Email, CancellationToken.None), renewalTime);
        connectedEmail.SubsRenewalJobId = RenewalJobId;

        if (save) await _appDbContext.SaveChangesAsync();
    }

    public async Task TagFailedMessagesMSFT(List<MsftMessage> failedMessages, string brokerEmail)
    {
        try
        {

            foreach (var mess in failedMessages)
            {
                await _aDGraphWrapper._graphClient.Users[brokerEmail]
                .Messages[mess.Id]
                .PatchAsync(new MsftMessage
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
            _logger.LogError("{tag} failed with error {error}", TagConstants.tagFailedMessages, er.Error.Code + ": " + er.Error.Message);
        }
    }

    /// <summary>
    /// failed messages since up to a week ago, except the ones just marked as failed now
    /// </summary>
    /// <param name = "failedMessages" ></ param >
    /// < returns ></ returns >
    public async Task<List<MsftMessage>> GetFailedMessagesMsft(string email, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }
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
            var res = failedMessages1.Value;
            return res;
        }
        catch (ODataError er)
        {
            _logger.LogError("{tag} failed with error {error}", TagConstants.getFailedMessages, er.Error.Code + ": " + er.Error.Message);
            return null;
        }
    }

    //msft
    public async Task ProcessMSFTAsync(EmailProcessingRefDTO refDTO, BrokerEmailProcessingDTO brokerDTO, int pageSize, string date, CancellationToken cancellationToken)
    {
        var messages = await _aDGraphWrapper._graphClient
                        .Users[brokerDTO.BrokerEmail]
                        .MailFolders["Inbox"]
                        .Messages
                        .GetAsync(config =>
                        {
                            config.QueryParameters.Top = pageSize;
                            config.QueryParameters.Select = new string[] { "id", "from", "subject", "isRead", "conversationId", "receivedDateTime", "replyTo", "body", "categories" };
                            config.QueryParameters.Filter = $"receivedDateTime gt {date}";
                            config.QueryParameters.Orderby = new string[] { "receivedDateTime" };
                            config.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"", "outlook.body-content-type=\"text\"" });
                        });

        var IDsALLToReprocessMailsThisRun = new List<string>();
        if (messages != null && messages.Value != null && messages.Value.Any())
        {
            bool first = true;
            do
            {
                if (!first)
                {
                    var nextPageRequestInformation = new RequestInformation
                    {
                        HttpMethod = Method.GET,
                        UrlTemplate = messages.OdataNextLink,
                    };
                    nextPageRequestInformation.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"", "outlook.body-content-type=\"text\"" });
                    messages = await _aDGraphWrapper._graphClient.RequestAdapter.SendAsync(nextPageRequestInformation, (parseNode) => new MessageCollectionResponse());
                }
                first = false;
                //process messages
                if (messages == null || messages.Value == null || !messages.Value.Any()) break;
                var messagesList = messages.Value;

                refDTO.LastProcessedTimestamp = (DateTimeOffset)messagesList.Last().ReceivedDateTime;
                var thisBatcFailedMessages = new List<MsftMessage>();
                //will tag failed messages in thisBatcFailedMessages
                var toks1 = await ProcessMessagesMSFTAsync(messagesList, brokerDTO, thisBatcFailedMessages);
                IDsALLToReprocessMailsThisRun.AddRange(thisBatcFailedMessages.Select(e => e.Id));
                refDTO.totaltokens += toks1;
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            } while (messages.OdataNextLink != null);
        }
        else refDTO.LastProcessedTimestamp = DateTimeOffset.UtcNow;

        //failed messages
        bool processFailed = GlobalControl.ProcessFailedEmailsParsing;
        if (cancellationToken.IsCancellationRequested)
        {
            processFailed = false;
        }
        //these would INCLUDE this run's failed messages
        var failedMessages1 = await GetFailedMessagesMsft(brokerDTO.BrokerEmail, cancellationToken);
        if (failedMessages1 == null) failedMessages1 = new List<MsftMessage>(1);
        var failedMessagesToProcess = failedMessages1.Where(failed => !IDsALLToReprocessMailsThisRun.Contains(failed.Id)).ToList();
        if (failedMessagesToProcess != null && failedMessagesToProcess.Count > 0)
        {
            if (processFailed)//want to process failed messsages
            {
                var faultedReprocess = new List<MsftMessage>();
                var toks = await ProcessMessagesMSFTAsync(failedMessagesToProcess, brokerDTO, faultedReprocess);
                refDTO.totaltokens += toks;
                foreach (var finalMessage in failedMessagesToProcess)
                {
                    if (faultedReprocess.Contains(finalMessage))
                        _logger.LogError("{tag} email with id {emailId} failed processing twice for email {email}", "emailProcessingFailed2ndTime", finalMessage.Id, brokerDTO.BrokerEmail);

                    await _aDGraphWrapper._graphClient.Users[brokerDTO.BrokerEmail]
                    .Messages[finalMessage.Id]
                    .PatchAsync(new MsftMessage
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
            else //if not processing failed messages, mark those who were marked as such as not failed
            //since dont want to return to them plus tard
            {
                foreach (var markSuccessMessage in failedMessages1)
                {
                    await _aDGraphWrapper._graphClient.Users[brokerDTO.BrokerEmail]
                    .Messages[markSuccessMessage.Id]
                    .PatchAsync(new MsftMessage
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
    }

    //msft
    /// <summary>
    /// needs brokerID
    /// </summary>
    /// <param name="message"></param>
    /// <param name="brokerEmail"></param>
    /// <returns></returns>
    public async Task<EmailEvent?> CreateEmailEventKnownLeadMsft(MsftMessage message, string brokerEmail, int leadId)
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
              config.QueryParameters.Filter = $"receivedDateTime gt {date} and conversationId eq '{message.ConversationId}'";
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
            TimeReceived = ((DateTimeOffset)message.ReceivedDateTime).UtcDateTime,
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
    public async Task<EmailparserDBRecrodsRes> FetchListingAndCreateDBRecordsAsync(LeadParsingContent parsedContent, bool FromLeadProvider, BrokerEmailProcessingDTO brokerDTO, MsftMessage? msftMessage, GmailMessageDecoded? gmailDecodedMessage)
    {
        using var context = _contextFactory.CreateDbContext();

        parsedContent.emailAddress = parsedContent.emailAddress?.ToLower() == "null" ? null : parsedContent.emailAddress;
        parsedContent.StreetAddress = parsedContent.StreetAddress?.ToLower() == "null" ? null : parsedContent.StreetAddress;
        parsedContent.PropertyAddress = parsedContent.PropertyAddress?.ToLower() == "null" ? null : parsedContent.PropertyAddress;
        parsedContent.Apartment = parsedContent.Apartment?.ToLower() == "null" ? null : parsedContent.Apartment;
        parsedContent.Language = parsedContent.Language?.ToLower() == "null" ? null : parsedContent.Language;
        parsedContent.phoneNumber = parsedContent.phoneNumber?.ToLower() == "null" ? null : parsedContent.phoneNumber;

        string messageFrom = gmailDecodedMessage?.From ?? msftMessage?.From.EmailAddress.Address;
        string messageId = gmailDecodedMessage?.message.Id ?? msftMessage.Id;
        bool? emailRead = gmailDecodedMessage?.isRead ?? msftMessage.IsRead;
        DateTime timeReceived = gmailDecodedMessage?.timeReceivedUTC ?? ((DateTimeOffset)msftMessage.ReceivedDateTime).UtcDateTime;
        string threadId = gmailDecodedMessage?.message.ThreadId ?? msftMessage.ConversationId;

        var result = new EmailparserDBRecrodsRes();
        string LeadEmail = "";
        if (FromLeadProvider)
        {
            var valid = false;
            if (!string.IsNullOrEmpty(parsedContent.emailAddress))
            {
                try
                {
                    var emailAddress = new MailAddress(parsedContent.emailAddress);
                    valid = true;
                    LeadEmail = parsedContent.emailAddress;
                }
                catch
                {
                }
            }
            if (!valid)
            {
                LeadEmail = messageFrom;
                result.LeadEmailUnsure = true;
                _logger.LogWarning("{tag} lead provider, gpt parsed email is {parsedEmail}, from email: {fromEmail}", TagConstants.createDbRecords, parsedContent.emailAddress, messageFrom);
            }
            //LeadEmail = message.ReplyTo?.FirstOrDefault()?.EmailAddress?.Address;
        }
        else //includes cases where email is actually from lead provider but has been forwarded, or 
        //when email is from lead provider that is not known by you yet
        {
            LeadEmail = messageFrom;
            if (!string.IsNullOrEmpty(parsedContent.emailAddress))
            {
                bool valid = false;
                try
                {
                    var emailAddress = new MailAddress(parsedContent.emailAddress);
                    valid = true;
                    LeadEmail = parsedContent.emailAddress;
                }
                catch
                {
                }
                if (valid && parsedContent.emailAddress != LeadEmail)
                {
                    result.LeadEmailUnsure = true;
                    _logger.LogWarning("{tag} Not lead provider, parsed email doesnt correspond to from. parsed: {parsedEmail}, from: {fromEmail}", TagConstants.createDbRecords, parsedContent.emailAddress, messageFrom);
                }
            }
        }

        bool listingFound = false;
        bool multipleListingsMatch = false;

        int ListingId = 0;
        Address? address = null;
        List<BrokerListingAssignment>? brokersAssigned = null;
        //(int ListingId, Address? address, string? gptAddress, List<BrokerListingAssignment>? brokersAssigned) MatchingListingTuple = (0, null, null, null);
        if (!string.IsNullOrEmpty(parsedContent.PropertyAddress) && !string.IsNullOrEmpty(parsedContent.StreetAddress))
        {
            //get listing
            var streetAddressFormatted = parsedContent.StreetAddress.FormatStreetAddress();
            var listings = await context.Listings
                .Include(l => l.BrokersAssigned)
                .Where(x => x.AgencyId == brokerDTO.AgencyId && EF.Functions.Like(x.FormattedStreetAddress, $"{streetAddressFormatted}%"))
                .AsNoTracking()
                .ToListAsync();
            //TODO if doesnt work try searching only with building number and then asking gpt like exlpained in 'complicated way'
            //in the text file, for now this is the simple way

            if (listings != null && listings.Count == 1) //1 listing matches, bingo!
            {
                ListingId = listings[0].Id;
                address = listings[0].Address;
                brokersAssigned = listings[0].BrokersAssigned;
                listingFound = true;
            }
            else if (listings.Count > 1) //more than 1 match
            {
                if (!string.IsNullOrEmpty(parsedContent.Apartment))
                {
                    var lis = listings.FirstOrDefault(a => a.Address.apt == parsedContent.Apartment);
                    if (lis != null)
                    {
                        ListingId = lis.Id;
                        address = lis.Address;
                        brokersAssigned = lis.BrokersAssigned;
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
        bool parsedSuccess = false;
        if (parsedContent.Language != null) parsedSuccess = Enum.TryParse(parsedContent.Language, true, out lang);
        if (!parsedSuccess && parsedContent.Language != null)
        {
            if (parsedContent.Language.ToLower().Contains("en")) lang = Language.English;
            else if (parsedContent.Language.ToLower().Contains("fr")) lang = Language.French;
        }

        if (parsedContent.phoneNumber != null)
        {
            parsedContent.phoneNumber = string.Concat(parsedContent.phoneNumber.
                Where(c => !char.IsWhiteSpace(c) && c != '(' && c != ')' && c != '-' && c != '_'));
        }

        var lead = new Lead
        {
            AgencyId = brokerDTO.AgencyId,
            LeadFirstName = (string.IsNullOrEmpty(parsedContent.firstName) || string.IsNullOrWhiteSpace(parsedContent.firstName) || parsedContent.firstName.Trim() == "null") ? "unknown" : parsedContent.firstName,
            LeadLastName = (string.IsNullOrEmpty(parsedContent.lastName) || string.IsNullOrWhiteSpace(parsedContent.lastName) || parsedContent.lastName.Trim() == "null") ? "unknown" : parsedContent.lastName,
            PhoneNumber = parsedContent.phoneNumber,
            EntryDate = DateTime.UtcNow - TimeSpan.FromSeconds(1),
            leadType = LeadType.Unknown,
            source = LeadSource.emailAuto,
            LeadStatus = LeadStatus.Hot,
            LeadEmails = new() { new LeadEmail { EmailAddress = LeadEmail, IsMain = true }},
            Language = lang,
        };
        lead.SourceDetails[NotificationJSONKeys.CreatedByFullName] = brokerDTO.brokerFirstName + " " + brokerDTO.brokerLastName;
        lead.SourceDetails[NotificationJSONKeys.CreatedById] = brokerDTO.Id.ToString();
        if (result.LeadEmailUnsure) lead.verifyEmailAddress = true;
        result.Lead = lead;

        AppEvent LeadCreationNotif = new()
        {
            EventTimeStamp = DateTime.UtcNow - TimeSpan.FromSeconds(1),
            DeleteAfterProcessing = false,
            ProcessingStatus = ProcessingStatus.NoNeed,
            ReadByBroker = false,
            BrokerId = brokerDTO.Id,
            EventType = EventType.LeadCreated,
        };
        LeadCreationNotif.Props[NotificationJSONKeys.EmailId] = messageId;
        lead.AppEvents = new() { LeadCreationNotif };

        var emailEvent = new EmailEvent
        {
            Id = messageId,
            BrokerId = brokerDTO.Id,
            LeadParsedFromEmail = true,
            BrokerEmail = brokerDTO.BrokerEmail,
            Seen = (bool)emailRead,
            TimeReceived = timeReceived,
            NeedsAction = !FromLeadProvider, //method called when mess from leadProvider OR new lead 
            //directly messaging. When directly messaging it needs reply.
        };
        if (FromLeadProvider) emailEvent.LeadProviderEmail = messageFrom;
        else emailEvent.ConversationId = threadId;
        lead.EmailEvents = new() { emailEvent };

        if (brokerDTO.isSolo || !brokerDTO.isAdmin) //solo broker or non admin
        {
            //always assign lead to self, if no listing found THAT IS ASSIGNED
            //TO SELF for non-admin non-solo broker then no listing linked, if listing found for soloBroker always link
            LeadCreationNotif.EventType = EventType.LeadCreated | EventType.LeadAssignedToYou;
            lead.BrokerId = brokerDTO.Id;
            if (listingFound)
            {
                if (brokerDTO.isSolo || (!brokerDTO.isAdmin && brokersAssigned.Any(x => x.BrokerId == brokerDTO.Id)))
                { lead.ListingId = ListingId; }
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
                lead.ListingId = ListingId;
                if (brokersAssigned != null && brokersAssigned.Count == 1)
                {
                    var brokerToAssignToId = brokersAssigned[0].BrokerId;
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
                            EventTimeStamp = DateTime.UtcNow - TimeSpan.FromSeconds(1),
                            DeleteAfterProcessing = false,
                            ProcessingStatus = ProcessingStatus.NoNeed,
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
    public int HandleTaskResultMsft(Task<OpenAIResponse?> leadTask, MsftMessage message, List<Tuple<Task<EmailparserDBRecrodsRes>, MsftMessage>> DBRecordsTasks, bool FromLeadProvider, BrokerEmailProcessingDTO brokerDTO, List<MsftMessage> ReprocessMessages)
    {
        if (leadTask.IsFaulted) //Task Error : this shouldnt happen as there is try catch block inside tasks
        {
            //TODO check error type to discard email if needed
            ReprocessMessages.Add(message);
            //TODO change error message if email discarded
            _logger.LogError("{tag} task faulted and error {Error}", TagConstants.handleTaskResult, leadTask?.Exception?.Message + leadTask?.Exception?.StackTrace);
            return 0;
        }

        var result = leadTask.Result;
        if (result == null)
        {
            _logger.LogError("{tag} HandleTaskResult result is null. discarding email", "HandleTaskResult");
            return 0;
        }

        if (!result.Success)
        {
            _logger.LogError("{tag} HandleTaskResult adding to ReprocessMessages, open ai parsing did not succeed." +
                " email parsing props: message : '{errorMessage}' and  type: '{errorType}'.", "HandleTaskResult", result.ErrorMessage, result.ErrorType);
            //TODO check error type to discard email if needed
            ReprocessMessages.Add(message);
        }
        else if (result.HasLead && result.content != null) //no error and has lead
        {
            DBRecordsTasks.Add(new Tuple<Task<EmailparserDBRecrodsRes>, MsftMessage>(FetchListingAndCreateDBRecordsAsync(result.content, FromLeadProvider, brokerDTO, message, null), message));
        }
        else if (result.HasLead && result.content == null)
        {
            //error its null
            _logger.LogError("{tag} HandleTaskResult has lead but result.content is null, open ai parsing did not succeed." +
                " for messageId {messageId}.", "HandleTaskResult", result.ProcessedMessageMSFT.Id);
        }
        else
        {

            //discard email, no lead found
        }
        return result.EmailTokensUsed;
    }


    //msft
    /// <summary>
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="isAdmin"></param>
    /// <param name="brokerId"></param>
    /// <param name="SoloBroker"></param>
    /// <param name="brokerEmail"></param>
    /// <returns>number of tokens used</returns>
    public async Task<int> ProcessMessagesMSFTAsync(List<MsftMessage> messagesUnfiltered, BrokerEmailProcessingDTO brokerDTO, List<MsftMessage> ReprocessMessages)
    {
        using var localdbContext = _contextFactory.CreateDbContext();
        var LeadIDsToStopActionPlan = new List<int>();
        int tokens = 0;
        var KnownLeadEmailEvents = new List<EmailEvent>();
        var KnownLeadTasks = new List<Tuple<Task<EmailEvent?>, MsftMessage>>();

        var messages = messagesUnfiltered.Where(m => !EmailSenderIgnore(m.From.EmailAddress.Address, brokerDTO.BrokerEmail));
        var groupedMessagesBySender = messages.GroupBy(m => m.From.EmailAddress.Address);

        var GroupedleadProviderEmails = groupedMessagesBySender.Where(g => GlobalControl.LeadProviderEmails.Contains(g.Key));
        List<Task<OpenAIResponse?>> LeadProviderTasks = new();
        List<MsftMessage> LeadProviderTaskMessages = new();
        foreach (var emailsGrouping in GroupedleadProviderEmails)
        {
            string fromEmailAddress = emailsGrouping.Key;
            foreach (var email in emailsGrouping)
            {
                LeadProviderTasks.Add(_GPT35Service.ParseEmailAsync(email, null, brokerDTO.BrokerEmail, brokerDTO.brokerFirstName, brokerDTO.brokerLastName, true));
                LeadProviderTaskMessages.Add(email);
            }
        }

        List<Task<OpenAIResponse?>> UnknownSenderTasks = new();
        List<MsftMessage> UnknownSenderTaskMessages = new();
        foreach (var messageGrp in groupedMessagesBySender)
        {
            string fromEmailAddress = messageGrp.Key;
            if (GlobalControl.LeadProviderEmails.Contains(fromEmailAddress)) continue;

            //TODO cache this
            var leadEmail = await localdbContext.LeadEmails
                .Select(le => new { le.EmailAddress, le.LeadId, le.Lead.HasActionPlanToStop, le.Lead.BrokerId })
                .FirstOrDefaultAsync(em => em.EmailAddress == fromEmailAddress && em.BrokerId == brokerDTO.Id);

            if (leadEmail != null)
            {
                if (leadEmail.HasActionPlanToStop) LeadIDsToStopActionPlan.Add(leadEmail.LeadId);
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
                            TimeReceived = ((DateTimeOffset)m.ReceivedDateTime).UtcDateTime,
                        }));
                    }
                    else
                    { //just 1 message, check that its not in a conversation
                        KnownLeadTasks.Add(new Tuple<Task<EmailEvent?>, MsftMessage>(CreateEmailEventKnownLeadMsft(convo.First(), brokerDTO.BrokerEmail, leadEmail.LeadId), convo.First()));
                    }
                }
            }
            else // email is from unknown, send to chat gpt
            {
                foreach (var email in messageGrp)
                {
                    //TODO take into consideration that this unknown sender might send multiple messages
                    if (email.Body.Content.Length < 6000)
                    {
                        UnknownSenderTasks.Add(_GPT35Service.ParseEmailAsync(email, null, brokerDTO.BrokerEmail, brokerDTO.brokerFirstName, brokerDTO.brokerLastName, false));
                        UnknownSenderTaskMessages.Add(email);
                    }
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

        List<Tuple<Task<EmailparserDBRecrodsRes>, MsftMessage>> LeadProviderDBRecordsTasks = new(LeadProviderTasks.Count);
        for (int i = 0; i < LeadProviderTasks.Count; i++)
        {
            var leadTask = LeadProviderTasks[i];
            var message = LeadProviderTaskMessages[i];
            tokens += HandleTaskResultMsft(leadTask, message, LeadProviderDBRecordsTasks, true, brokerDTO, ReprocessMessages);
        }
        try
        {
            await Task.WhenAll(UnknownSenderTasks);
        }
        catch { }

        List<Tuple<Task<EmailparserDBRecrodsRes>, MsftMessage>> UnknownDBRecordsTasks = new(UnknownSenderTasks.Count);
        for (int i = 0; i < UnknownSenderTasks.Count; i++)
        {
            var leadTask = UnknownSenderTasks[i];
            var message = UnknownSenderTaskMessages[i];
            tokens += HandleTaskResultMsft(leadTask, message, UnknownDBRecordsTasks, false, brokerDTO, ReprocessMessages);
        }
        //--------------------

        //analyzing chatGPT results
        List<Tuple<EmailparserDBRecrodsRes, MsftMessage>> leadsAdded = new(LeadProviderDBRecordsTasks.Count + UnknownDBRecordsTasks.Count);
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
                _logger.LogError("{tag} lead provider dbRecordsCreation and error {Error}", TagConstants.createDbRecordsResults, LeadProviderDBRecordsTask.Item1.Exception.Message + LeadProviderDBRecordsTask.Item1.Exception.StackTrace);
            }
            else
            {
                var Newlead = LeadProviderDBRecordsTask.Item1.Result.Lead;
                bool exists = false;
                //only relevant for admins, unassigned lead
                //check if exists in agency
                //if (Newlead.BrokerId == null) exists = await localdbContext.LeadEmails.AnyAsync(e => e.EmailAddress == Newlead.LeadEmails.First().EmailAddress && e.Lead.AgencyId == brokerDTO.AgencyId);
                //relevant for brokers and admins who assigned lead to another broker or themselve
                //else exists = await localdbContext.LeadEmails.AnyAsync(e => e.EmailAddress == Newlead.LeadEmails.First().EmailAddress && e.Lead.BrokerId == Newlead.BrokerId);
                exists = await localdbContext.LeadEmails.AnyAsync(e => e.EmailAddress == Newlead.LeadEmails.First().EmailAddress && e.Lead.AgencyId == brokerDTO.AgencyId);
                if (!exists)
                {
                    localdbContext.Leads.Add(Newlead);
                    leadsAdded.Add(new Tuple<EmailparserDBRecrodsRes, MsftMessage>(LeadProviderDBRecordsTask.Item1.Result, LeadProviderDBRecordsTask.Item2));
                }
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
                var errMessage = UnknownDBRecordsTask.Item1?.Exception?.Message ?? "null message";
                var stackTrace = UnknownDBRecordsTask.Item1?.Exception?.InnerException?.StackTrace ?? "null stackTrace";
                _logger.LogError("{tag} Unknown sender dbRecordsCreation for {messageId} and error {Error}", TagConstants.createDbRecordsResults, UnknownDBRecordsTask.Item2, errMessage + stackTrace);
            }
            else
            {
                var Newlead = UnknownDBRecordsTask.Item1.Result.Lead;
                bool exists = false;
                //only relevant for admins
                //if (Newlead.BrokerId == null) exists = await localdbContext.LeadEmails.AnyAsync(e => e.EmailAddress == Newlead.LeadEmails.First().EmailAddress && e.Lead.AgencyId == brokerDTO.AgencyId);
                //relevant for brokers and admins who assigned lead to another broker or themselve
                //else exists = await localdbContext.LeadEmails.AnyAsync(e => e.EmailAddress == Newlead.LeadEmails.First().EmailAddress && e.Lead.BrokerId == Newlead.BrokerId);
                exists = await localdbContext.LeadEmails.AnyAsync(e => e.EmailAddress == Newlead.LeadEmails.First().EmailAddress && e.Lead.AgencyId == brokerDTO.AgencyId);
                if (!exists)
                {
                    localdbContext.Leads.Add(Newlead);
                    leadsAdded.Add(new Tuple<EmailparserDBRecrodsRes, MsftMessage>(UnknownDBRecordsTask.Item1.Result, UnknownDBRecordsTask.Item2));
                }
            }
        }
        //transaction-------------------------------
        using var transaction = await localdbContext.Database.BeginTransactionAsync();

        //TODO add events/emails that are not attached to lead to the dbcontext

        //Dictionary<int, int> listingIdToNewLeadCount = new();
        //leadsAdded.Where(l => l.Item1.Lead.ListingId != null).GroupBy(l => l.Item1.Lead.ListingId).ToList().ForEach(g => listingIdToNewLeadCount.Add((int)g.Key, g.Count()));

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

        var ActionPlanEvents = new List<AppEvent>();
        //Stop action plans for leads that replied
        if (LeadIDsToStopActionPlan.Any())
        {
            var ActionPlanSuccesdict = new Dictionary<int, int>();//action plan id, times response
            foreach (var leadId in LeadIDsToStopActionPlan)
            {
                var ActionPlanAssociations = await localdbContext.ActionPlanAssociations
                    .Include(apa => apa.ActionPlan)
                    .Include(apa => apa.ActionTrackers.Where(a => a.ActionStatus == ActionStatus.ScheduledToStart || a.ActionStatus == ActionStatus.Failed))
                    .Where(apa => apa.LeadId == leadId && apa.ThisActionPlanStatus == ActionPlanStatus.Running && apa.ActionPlan.StopPlanOnInteraction)
                    .ToListAsync();
                //will probably always be 1 at the beginning
                foreach (var apass in ActionPlanAssociations)
                {
                    var APStopppedEvent = StopActionPlan(brokerDTO.Id, apass);
                    if (APStopppedEvent != null)
                    {
                        ActionPlanEvents.Add(APStopppedEvent);
                        if (ActionPlanSuccesdict.ContainsKey((int)apass.ActionPlanId))
                            ActionPlanSuccesdict[(int)apass.ActionPlanId]++;
                        else ActionPlanSuccesdict.Add((int)apass.ActionPlanId, 1);
                    }
                }
            }
            localdbContext.AppEvents.AddRange(ActionPlanEvents);
            foreach (var keyValuePair in ActionPlanSuccesdict)
            {
                var actionPId = keyValuePair.Key;
                var value = keyValuePair.Value;
                bool saved = false;
                byte count = 0;
                while (!saved && count <= 3)
                {
                    try
                    {
                        count++;
                        await _appDbContext.ActionPlans.Where(ap => ap.Id == actionPId)
                        .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(e => e.TimesSuccess, e => e.TimesSuccess + value));
                        saved = true;
                    }
                    catch (Exception ex)
                    {
                        await Task.Delay(300);
                    }
                }
            }
        }

        await localdbContext.SaveChangesAsync();

        //later maybe admin can define action plans that run on unassigned leads
        var assignedAddedLeads = leadsAdded.Where(l => l.Item1.Lead.BrokerId != null);
        //Trigger Action plans start
        int TimesActionPlanUsed = 0;
        if (assignedAddedLeads.Any())
        {
            //localdbContext.Leads.AddRange(leadsAdded.Select(l => l.Item1.Lead));            
            if (brokerDTO.brokerStartActionPlans.Any())
            {
                var actionPlan = brokerDTO.brokerStartActionPlans[0];
                foreach (var leadT in assignedAddedLeads)
                {
                    if (leadT.Item1.LeadEmailUnsure) continue;
                    var lead = leadT.Item1.Lead;
                    var LeadAssignmentEvent = lead.AppEvents.FirstOrDefault(e => e.EventType.HasFlag(EventType.LeadAssignedToYou));
                    if (LeadAssignmentEvent != null)
                    {
                        var added = TriggerActionPlan(actionPlan, lead, brokerDTO.Id);
                        localdbContext.Entry(lead).State = EntityState.Modified;
                        ActionPlanEvents.Add(added);
                        TimesActionPlanUsed++;
                        //has new ActionPlanAssociation
                        //and appEVent
                    }
                }
                await localdbContext.SaveChangesAsync();
            }
        }
        if (TimesActionPlanUsed > 0)
        {
            bool saved = false;
            byte count = 0;
            while (!saved && count <= 3)
            {
                try
                {
                    count++;
                    await _appDbContext.ActionPlans.Where(ap => ap.Id == brokerDTO.brokerStartActionPlans[0].Id)
                    .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(e => e.TimesUsed, e => e.TimesUsed + TimesActionPlanUsed));
                    saved = true;
                }
                catch (Exception ex)
                {
                    await Task.Delay(300);
                }
            }
        }
        //mark the messages that had a lead with "LeadExtracted"
        await Task.WhenAll(leadsAdded.Select(async (tup) =>
        {
            try
            {
                if (tup.Item2.Categories == null) tup.Item2.Categories = new();
                if (!tup.Item2.Categories.Any(c => c == APIConstants.NewLeadCreated))
                {
                    tup.Item2.Categories.Add(APIConstants.NewLeadCreated);
                    //if (tup.Item1.LeadEmailUnsure) tup.Item2.Categories.Add(APIConstants.VerifyEmailAddress);
                    await _aDGraphWrapper._graphClient.Users[brokerDTO.BrokerEmail].Messages[tup.Item2.Id]
                    .PatchAsync(tup.Item2);
                }
            }
            catch (ODataError ex)
            {
                _logger.LogError("{tag} adding 'leadExtracted' email category error: {Error}", TagConstants.emailCategory, ex.Error.Message + ": " + ex.Error.Code);
            }
        }));
        //TODO if neabled, forward all emails from leads to assigned brokers


        //mark the messages that failed with tag ReprocessMessageId"
        await TagFailedMessagesMSFT(ReprocessMessages, brokerDTO.BrokerEmail);

        await transaction.CommitAsync();
        //transaction-------------------------------
        var appevents = leadsAdded.SelectMany(tup => tup.Item1.Lead.AppEvents).ToList();
        appevents.AddRange(ActionPlanEvents);
        var emailevents = leadsAdded.SelectMany(tup => tup.Item1.Lead.EmailEvents).ToList();
        emailevents.AddRange(KnownLeadEmailEvents);
        await _realTimeNotif.SendRealTimeNotifsAsync(_logger, brokerDTO.Id, true, true, null, appevents, emailevents);
        return tokens;
    }

    // --------------------------------- MSFTENDS ----- COMMON STARTs --------------------

    //common
    public async Task CheckEmailSyncAsync(bool isMsft, Guid? SubsId = null, string? tenantId = null, string? gmailEmail = null)
    {
        //ADDCACHE
        if (isMsft)
        {
            var modSubsId = SubsId ?? Guid.Empty;
            if (StaticEmailConcurrencyHandler.EmailParsingdictMSFT.TryAdd(modSubsId, true))
            {
                var connEmail = await _appDbContext.ConnectedEmails.FirstOrDefaultAsync(e => e.GraphSubscriptionId == modSubsId);
                //'lock' obtained by putting subsID as key in dictionary
                if (connEmail == null)
                {
                    _logger.LogError("{tag} null connEmail with subsId {susbId}", "CheckEmailSyncAsync", modSubsId);
                    StaticEmailConcurrencyHandler.EmailParsingdictMSFT.TryRemove(modSubsId, out var s);
                    return;
                }
                string jobId = "";
                try
                {
                    jobId = BackgroundJob.Schedule<EmailProcessor>(e => e.SyncEmailAsync(connEmail.Email, null, CancellationToken.None), GlobalControl.EmailStartSyncingDelayMsft);
                }
                catch (Exception ex)
                {
                    _logger.LogError("{tag} error scheduling email parsing with error {error}", TagConstants.HangfireScheduleEmailParser, ex.Message + ex.StackTrace);
                    StaticEmailConcurrencyHandler.EmailParsingdictMSFT.TryRemove(modSubsId, out var s);
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
                    _logger.LogError("{tag} error saving db after scheduling email parsing with error {error}", TagConstants.scheduleEmailParser, ex.Message);

                    BackgroundJob.Delete(jobId);
                    StaticEmailConcurrencyHandler.EmailParsingdictMSFT.TryRemove(modSubsId, out var s);
                }
            }
        }
        else
        {
            var modGmailEmail = gmailEmail ?? "";
            if (StaticEmailConcurrencyHandler.EmailParsingdictGMAIL.TryAdd(modGmailEmail, true))
            {
                var connEmail = await _appDbContext.ConnectedEmails.FirstOrDefaultAsync(e => e.Email == modGmailEmail && !e.isMSFT);
                //'lock' obtained by putting gmail email as key in dictionary
                if (connEmail == null)
                {
                    _logger.LogError("{tag} null connEmail with email {email}", "CheckEmailSyncAsync", modGmailEmail);
                    StaticEmailConcurrencyHandler.EmailParsingdictGMAIL.TryRemove(modGmailEmail, out var s);
                    return;
                }
                string jobId = "";
                try
                {
                    jobId = BackgroundJob.Schedule<EmailProcessor>(e => e.SyncEmailAsync(connEmail.Email, null, CancellationToken.None), GlobalControl.EmailStartSyncingDelayGmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError("{tag} error scheduling email parsing with error {error}", TagConstants.HangfireScheduleEmailParser, ex.Message + ex.StackTrace);
                    StaticEmailConcurrencyHandler.EmailParsingdictGMAIL.TryRemove(modGmailEmail, out var s);
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
                    _logger.LogError("{tag} error saving db after scheduling email parsing with error {error}", TagConstants.scheduleEmailParser, ex.Message);

                    BackgroundJob.Delete(jobId);
                    StaticEmailConcurrencyHandler.EmailParsingdictGMAIL.TryRemove(modGmailEmail, out var s);
                }
            }
        }
    }
    //common
    public static bool EmailSenderIgnore(string senderAddress, string brokerAddress)
    {
        senderAddress = senderAddress.ToLower();
        if (brokerAddress == senderAddress) return true;
        if (GlobalControl.ProcessingIgnoreEmails.Contains(senderAddress)) return true;
        var domain = senderAddress.Split('@')[1];
        foreach (var domainIgnore in GlobalControl.ProcessingIgnoreDomains)
        {
            if (domain == domainIgnore) return true;
        }
        return false;
    }
    //common
    public class EmailProcessingRefDTO
    {
        public DateTimeOffset LastProcessedTimestamp { get; set; }
        public int totaltokens { get; set; }
        public string? historyId { get; set; }
    }
    //common
    /// <summary>
    /// fetch all emails from last sync date and process them
    /// </summary>
    /// <param name="connEmailId"></param>
    /// <param name="tenantId"></param>
    public async Task SyncEmailAsync(string email, PerformContext performContext, CancellationToken cancellationToken)
    {
        if (_webHostEnv.IsDevelopment()) _logger.LogInformation("started procesing");
        //TODO Cache
        using (LogContext.PushProperty("hanfireJobId", performContext.BackgroundJob.Id))
        using (LogContext.PushProperty("brokerEmail", email))
        {
            var connEmail = await _appDbContext.ConnectedEmails
          .Select(e => new { e.isMSFT, e.AccessToken, e.historyId, e.SyncJobId, e.Email, e.GraphSubscriptionId, e.LastSync, e.tenantId, e.AssignLeadsAuto, e.Broker.Language, e.OpenAITokensUsed, e.BrokerId, e.Broker.isAdmin, e.Broker.AgencyId, e.Broker.isSolo, e.Broker.FirstName, e.Broker.LastName })
          .FirstAsync(x => x.Email == email);

            var brokerStartActionPlans = await _appDbContext.ActionPlans
                .Include(ap => ap.Actions.Where(a => a.ActionLevel == 1))
                .Where(ap => ap.BrokerId == connEmail.BrokerId && ap.isActive && ap.Triggers.HasFlag(EventType.LeadAssignedToYou))
                .AsNoTracking()
                .ToListAsync();

            if (connEmail.isMSFT)
            {
                _aDGraphWrapper.CreateClient(connEmail.tenantId);
                if (StaticEmailConcurrencyHandler.EmailParsingdictMSFT.TryGetValue((Guid)connEmail.GraphSubscriptionId, out var s))
                {
                    if (performContext.BackgroundJob.Id != connEmail.SyncJobId)
                    {
                        _logger.LogWarning("{tag} sync email job's connectedEmail syncJobId {dbSyncJobID} not equal to actual jobId {currentJobId}.", TagConstants.syncEmail, connEmail.SyncJobId, performContext.BackgroundJob.Id);
                        StaticEmailConcurrencyHandler.EmailParsingdictMSFT.TryRemove((Guid)connEmail.GraphSubscriptionId, out var ss);
                        return;
                    }
                }
                else
                {
                    _logger.LogWarning("{tag} Msft  sync email job started without SubsID {subsId} in dictionary,returning.", TagConstants.syncEmail, connEmail.GraphSubscriptionId);
                    StaticEmailConcurrencyHandler.EmailParsingdictMSFT.TryRemove((Guid)connEmail.GraphSubscriptionId, out var ss);
                    return;
                }
            }
            else
            {
                if (StaticEmailConcurrencyHandler.EmailParsingdictGMAIL.TryGetValue(email, out var s))
                {
                    if (performContext.BackgroundJob.Id != connEmail.SyncJobId)
                    {
                        _logger.LogWarning("{tag} sync email job's connectedEmail syncJobId {dbSyncJobID} not equal to actual jobId {currentJobId}.", TagConstants.syncEmail, connEmail.SyncJobId, performContext.BackgroundJob.Id);
                        StaticEmailConcurrencyHandler.EmailParsingdictGMAIL.TryRemove(email, out var ss);
                        return;
                    }
                }
                else
                {
                    _logger.LogWarning("{tag} Gmail sync email job started without email {email} in dictionary,returning.", TagConstants.syncEmail, connEmail.Email);
                    StaticEmailConcurrencyHandler.EmailParsingdictGMAIL.TryRemove(email, out var ss);
                    return;
                }
            }

            var brokerDTO = new BrokerEmailProcessingDTO
            { isMsft = connEmail.isMSFT, accessToken = connEmail.AccessToken, Id = connEmail.BrokerId, brokerFirstName = connEmail.FirstName, brokerLastName = connEmail.LastName, AgencyId = connEmail.AgencyId, isAdmin = connEmail.isAdmin, isSolo = connEmail.isSolo, BrokerEmail = connEmail.Email, BrokerLanguge = connEmail.Language, AssignLeadsAuto = connEmail.AssignLeadsAuto };
            if (brokerStartActionPlans.Count == 1) brokerDTO.brokerStartActionPlans = brokerStartActionPlans;
            else brokerDTO.brokerStartActionPlans = new();

            DateTimeOffset lastSync;
            if (connEmail.LastSync == null) lastSync = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(30);
            else
                lastSync = (DateTimeOffset)connEmail.LastSync + TimeSpan.FromSeconds(1);


            //TODO deal with when lastSync and FirstSync is null it means this is the first sync

            string date = lastSync.ToString("o");
            int totaltokens = 0;
            int pageSize = 8;

            DateTimeOffset LastProcessedTimestamp = lastSync;

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var RefsDTO = new EmailProcessingRefDTO
            {
                LastProcessedTimestamp = LastProcessedTimestamp,
                totaltokens = totaltokens,
                historyId = connEmail.historyId
            };

            try
            {
                if (connEmail.isMSFT)
                    await ProcessMSFTAsync(RefsDTO, brokerDTO, pageSize, date, cancellationToken);
                else
                    await ProcessGMAILAsync(RefsDTO, brokerDTO, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical("{tag} sync email job failed with error {error}", TagConstants.syncEmail, ex.Message + ": " + ex.StackTrace);
            }
            finally
            {
                totaltokens = RefsDTO.totaltokens;
                LastProcessedTimestamp = RefsDTO.LastProcessedTimestamp;

                if (connEmail.isMSFT)
                {
                    await _appDbContext.ConnectedEmails.Where(e => e.Email == email)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.OpenAITokensUsed, e => e.OpenAITokensUsed + totaltokens)
                    .SetProperty(e => e.SyncScheduled, false)
                    .SetProperty(e => e.LastSync, LastProcessedTimestamp.UtcDateTime));
                    StaticEmailConcurrencyHandler.EmailParsingdictMSFT.TryRemove((Guid)connEmail.GraphSubscriptionId, out var ss1);
                }
                else
                    await _appDbContext.ConnectedEmails.Where(e => e.Email == email)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.OpenAITokensUsed, e => e.OpenAITokensUsed + totaltokens)
                    .SetProperty(e => e.SyncScheduled, false)
                    .SetProperty(e => e.LastSync, LastProcessedTimestamp.UtcDateTime)
                    .SetProperty(e => e.historyId, RefsDTO.historyId));
                StaticEmailConcurrencyHandler.EmailParsingdictGMAIL.TryRemove(email, out var ss2);
            }
            if (_webHostEnv.IsDevelopment()) _logger.LogInformation("done procesing");
        }
    }


    // ---------------------------- COMMONENDS ---- GOOGLE Starts
    //TODO

    public async Task ProcessGMAILAsync(EmailProcessingRefDTO refDTO, BrokerEmailProcessingDTO brokerDTO, CancellationToken cancellationToken)
    {
        //TODO later : api only returns messages in DESC order so from most recent, in case there is error or backend stops
        //while processing, need custom logic to process emails in the time range that was faulted 
        //So Need to save After and Before timestamps for error range and then process them in the next run

        GoogleCredential cred = GoogleCredential.FromAccessToken(brokerDTO.accessToken);
        _GmailService = new GmailService(new BaseClientService.Initializer { HttpClientInitializer = cred });

        var originalLastPRocessedTimesTamp = refDTO.LastProcessedTimestamp;
        var CutoffTime = refDTO.LastProcessedTimestamp.ToUnixTimeSeconds();
        var messRequest = _GmailService.Users.Messages.List("me");
        messRequest.IncludeSpamTrash = false;
        messRequest.LabelIds = new string[] { "INBOX" };
        messRequest.MaxResults = 20;
        messRequest.Q = $"category:primary after:{CutoffTime}";

        var messagesPage = await messRequest.ExecuteAsync();
        var IDsALLToReprocessMailsThisRun = new List<string>();
        if (messagesPage == null || messagesPage.Messages == null)  goto FailedLabel;
        
        bool first = true;
        string NextPageToken = "";      
        do
        {
            if (!first)
            {
                messRequest = _GmailService.Users.Messages.List("me");
                messRequest.IncludeSpamTrash = false;
                messRequest.LabelIds = new string[] { "INBOX" };
                messRequest.MaxResults = 10;
                messRequest.Q = $"category:primary after:{CutoffTime}";
                messRequest.PageToken = NextPageToken;

                messagesPage = await messRequest.ExecuteAsync();
            }
            var gmailMessages = new List<GmailMessage>(messagesPage.Messages.Count);
            var batchRequest = new BatchRequest(_GmailService);

            messagesPage.Messages.ToList().ForEach(m =>
            {
                var getRequest = _GmailService.Users.Messages.Get("me", m.Id);
                getRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
                batchRequest.Queue<GmailMessage>(getRequest,
                 (content, error, i, message) =>
                 {
                     gmailMessages.Insert(i, content);
                 });
            });
            await batchRequest.ExecuteAsync();
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            if (first)
            {
                refDTO.LastProcessedTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(gmailMessages[0].InternalDate.Value) + TimeSpan.FromSeconds(1);
                refDTO.historyId = gmailMessages[0].HistoryId.ToString();
            }
            var messagesAfterPreicseCutoff = gmailMessages.Where(m => DateTimeOffset.FromUnixTimeMilliseconds(m.InternalDate.Value) >= originalLastPRocessedTimesTamp).ToList();
            if(messagesAfterPreicseCutoff == null || messagesAfterPreicseCutoff.Count == 0)
            {
                goto FailedLabel;
            }
            var thisBatchReprocess = new List<GmailMessage>();
            //process
            var tokens = await ProcessMessagesGmailAsync(messagesAfterPreicseCutoff, brokerDTO, thisBatchReprocess);
            refDTO.totaltokens += tokens;
            IDsALLToReprocessMailsThisRun.AddRange(thisBatchReprocess.Select(m => m.Id));

            NextPageToken = messagesPage.NextPageToken;
            first = false;
        } while (messagesPage.NextPageToken != null);
        
        FailedLabel:
        bool processFailed = GlobalControl.ProcessFailedEmailsParsing;
        if (cancellationToken.IsCancellationRequested)
        {
            processFailed = false;
        }
        //these would INCLUDE this run's failed messages
        var failedMessages1 = await GetFailedMessagesGMAILAsync(brokerDTO.BrokerEmail, cancellationToken);
        var failedMessagesToProcess = failedMessages1.Where(failed => !IDsALLToReprocessMailsThisRun.Contains(failed.Id)).ToList();
        if (failedMessagesToProcess != null && failedMessagesToProcess.Count > 0)
        {
            var labelsRes = await _GmailService.Users.Labels.List("me").ExecuteAsync();
            var labels = labelsRes.Labels.ToList();

            var reprocessLabel = labels.FirstOrDefault(l => l.Name == "SealDealReprocess");

            if (processFailed)//want to process failed messsages
            {
                var faultedReprocess = new List<GmailMessage>();
                var toks = await ProcessMessagesGmailAsync(failedMessagesToProcess, brokerDTO, faultedReprocess);
                refDTO.totaltokens += toks;
                foreach (var finalMessage in failedMessagesToProcess)
                {
                    if (faultedReprocess.Contains(finalMessage))
                        _logger.LogError("{tag} email with id {emailId} failed processing twice for email {email}", "emailProcessingFailed2ndTime", finalMessage.Id, brokerDTO.BrokerEmail);
                }

                if (reprocessLabel != null)
                {
                    await _GmailService.Users.Messages.BatchModify(new BatchModifyMessagesRequest
                    {
                        RemoveLabelIds = new List<string>() { reprocessLabel.Id },
                        Ids = failedMessagesToProcess.Select(m => m.Id).ToList()
                    }, "me").ExecuteAsync();
                }
            }
            else //if not processing failed messages, mark those who were marked as such as not failed
                 //since dont want to return to them plus tard
            {
                //mark ALL failedMessages1  as unfailed
                if (reprocessLabel != null)
                {
                    await _GmailService.Users.Messages.BatchModify(new BatchModifyMessagesRequest
                    {
                        RemoveLabelIds = new List<string>() { reprocessLabel.Id },
                        Ids = failedMessages1.Select(m => m.Id).ToList()
                    }, "me").ExecuteAsync();
                }
            }
        }
    }

    /// <summary>
    /// never returns null
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static MailAddress?[] ConvertGmailHeaderFieldToPeople(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
            return Array.Empty<MailAddress>();

        InternetAddressList addresses;
        if (InternetAddressList.TryParse(field, out addresses))
        {
            return addresses.Select(s =>
            {
                if (s is MailboxAddress)
                {
                    var i = s as MailboxAddress;
                    return new MailAddress(i.Address);
                }
                else if (s is GroupAddress)
                {
                    var i = s as GroupAddress;
                    return null;
                }
                else
                {
                    throw new NotImplementedException("Could not find SigParser code handler for address type " + s.GetType().FullName);
                }
            })
                .Where(a => a != null)
                .ToArray();
        }
        else
        {
            return Array.Empty<MailAddress>();
        }
    }

    public class GmailMessageDecoded
    {
        public GmailMessage message { get; set; }
        public string From { get; set; }
        public string textBody { get; set; }
        public bool isRead { get; set; }
        public DateTime timeReceivedUTC { get; set; }
    }

    public static Google.Apis.Gmail.v1.Data.MessagePart FindGmailBody(List<Google.Apis.Gmail.v1.Data.MessagePart>? parts)
    {
        if (parts == null || parts.Count == 0) return null;
        foreach (var part in parts)
        {
            if (part.MimeType == "text/plain" || part.MimeType == "text/html")
            {
                return part;
            }
            if (part.Parts != null)
            {
                var thisPartResults = FindGmailBody(part.Parts.ToList());
                if (thisPartResults != null) return thisPartResults;
            }
        }
        return null;
    }

    public static List<GmailMessageDecoded> DecodeGmail(List<GmailMessage> originalMessages, ILogger logger)
    {
        var result = new List<GmailMessageDecoded>(originalMessages.Count);
        foreach (var message in originalMessages)
        {
            var parts = message?.Payload?.Parts;
            if (parts == null || parts.Count == 0)
            {
                logger.LogWarning("{tag} Decoding gmail message with messageId {messageId} with no parts","DecodeGmail",message.Id);
                continue;
            }
            
            var part = FindGmailBody(parts.ToList());
            var bytes = WebEncoders.Base64UrlDecode(part.Body.Data);
            var decodedBody = Encoding.UTF8.GetString(bytes);
            if (part.MimeType == "text/html")
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(decodedBody);

                decodedBody =  htmlDoc.DocumentNode.InnerText;
            }

            var from = message.Payload.Headers.FirstOrDefault(h => h.Name == "From")?.Value;
            var fromDecoded = ConvertGmailHeaderFieldToPeople(from);
            if (fromDecoded.Length != 1)
                throw new Exception("lmao no sender or many"); //TODO handle
            var decoded = new GmailMessageDecoded
            {
                message = message,
                From = fromDecoded.FirstOrDefault().Address,
                textBody = decodedBody,
                isRead = !message.LabelIds.Contains("UNREAD"),
                timeReceivedUTC = DateTimeOffset.FromUnixTimeMilliseconds(message.InternalDate.Value).UtcDateTime
            };
            result.Add(decoded);
        }
        return result;
    }
    /// <summary>
    /// r
    /// </summary>
    /// <param name="gmailMessages"></param>
    /// <param name="brokerDTO"></param>
    /// <param name="reprocessMessages">list where to put messages that failed</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<int> ProcessMessagesGmailAsync(List<GmailMessage> messagesInput, BrokerEmailProcessingDTO brokerDTO, List<GmailMessage> ReprocessMessages)
    {
        using var localdbContext = _contextFactory.CreateDbContext();
        var LeadIDsToStopActionPlan = new List<int>();
        int tokens = 0;
        var KnownLeadEmailEvents = new List<EmailEvent>();
        var KnownLeadTasks = new List<Tuple<Task<EmailEvent?>, GmailMessageDecoded>>();

        var messagesUnfiltered = DecodeGmail(messagesInput,_logger);
        var messages = messagesUnfiltered.Where(m => !EmailSenderIgnore(m.From, brokerDTO.BrokerEmail)).ToList();
        var groupedMessagesBySender = messages.GroupBy(m => m.From);

        var GroupedleadProviderEmails = groupedMessagesBySender.Where(g => GlobalControl.LeadProviderEmails.Contains(g.Key));
        List<Task<OpenAIResponse?>> LeadProviderTasks = new();
        List<GmailMessageDecoded> LeadProviderTaskMessages = new();
        foreach (var emailsGrouping in GroupedleadProviderEmails)
        {
            string fromEmailAddress = emailsGrouping.Key;
            foreach (var email in emailsGrouping)
            {
                LeadProviderTasks.Add(_GPT35Service.ParseEmailAsync(null, email, brokerDTO.BrokerEmail, brokerDTO.brokerFirstName, brokerDTO.brokerLastName, true));
                LeadProviderTaskMessages.Add(email);
            }
        }

        List<Task<OpenAIResponse?>> UnknownSenderTasks = new();
        List<GmailMessageDecoded> UnknownSenderTaskMessages = new();
        foreach (var messageGrp in groupedMessagesBySender)
        {
            string fromEmailAddress = messageGrp.Key;
            if (GlobalControl.LeadProviderEmails.Contains(fromEmailAddress)) continue;

            //TODO cache this
            var leadEmail = await localdbContext.LeadEmails
                .Select(le => new { le.EmailAddress, le.LeadId, le.Lead.HasActionPlanToStop, le.Lead.BrokerId })
                .FirstOrDefaultAsync(em => em.EmailAddress == fromEmailAddress && em.BrokerId == brokerDTO.Id);

            if (leadEmail != null)
            {
                if (leadEmail.HasActionPlanToStop) LeadIDsToStopActionPlan.Add(leadEmail.LeadId);
                var groupedByConvo = messageGrp.GroupBy(m => m.message.ThreadId);
                foreach (var convo in groupedByConvo)
                {
                    if (convo.Count() > 1) //multiple messages in a conversation, need reply false so only create emailEvents for unseen emails
                    {
                        KnownLeadEmailEvents.AddRange(convo.Where(m => !m.isRead).Select(m => new EmailEvent
                        {
                            NeedsAction = false,
                            BrokerEmail = brokerDTO.BrokerEmail,
                            BrokerId = brokerDTO.Id,
                            Id = m.message.Id,
                            LeadId = leadEmail.LeadId,
                            LeadParsedFromEmail = false,
                            Seen = false,
                            TimeReceived = m.timeReceivedUTC,
                        }));
                    }
                    else
                    { //just 1 message, check that its not in a conversation
                        KnownLeadTasks.Add(new Tuple<Task<EmailEvent?>, GmailMessageDecoded>(CreateEmailEventKnownLeadGmail(convo.First(), brokerDTO.BrokerEmail, leadEmail.LeadId), convo.First()));
                    }
                }
            }
            else // email is from unknown, send to chat gpt
            {
                foreach (var email in messageGrp)
                {
                    //TODO take into consideration that this unknown sender might send multiple messages
                    if (email.textBody.Length < 6000 || _webHostEnv.IsDevelopment())
                    {
                        UnknownSenderTasks.Add(_GPT35Service.ParseEmailAsync(null, email, brokerDTO.BrokerEmail, brokerDTO.brokerFirstName, brokerDTO.brokerLastName, false));
                        UnknownSenderTaskMessages.Add(email);
                    }
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

        List<Tuple<Task<EmailparserDBRecrodsRes>, GmailMessageDecoded>> LeadProviderDBRecordsTasks = new(LeadProviderTasks.Count);
        for (int i = 0; i < LeadProviderTasks.Count; i++)
        {
            var leadTask = LeadProviderTasks[i];
            var message = LeadProviderTaskMessages[i];
            tokens += HandleTaskResultGmail(leadTask, message, LeadProviderDBRecordsTasks, true, brokerDTO, ReprocessMessages);
        }
        try
        {
            await Task.WhenAll(UnknownSenderTasks);
        }
        catch { }

        List<Tuple<Task<EmailparserDBRecrodsRes>, GmailMessageDecoded>> UnknownDBRecordsTasks = new(UnknownSenderTasks.Count);
        for (int i = 0; i < UnknownSenderTasks.Count; i++)
        {
            var leadTask = UnknownSenderTasks[i];
            var message = UnknownSenderTaskMessages[i];
            tokens += HandleTaskResultGmail(leadTask, message, UnknownDBRecordsTasks, false, brokerDTO, ReprocessMessages);
        }
        //--------------------

        //analyzing chatGPT results
        List<Tuple<EmailparserDBRecrodsRes, GmailMessageDecoded>> leadsAdded = new(LeadProviderDBRecordsTasks.Count + UnknownDBRecordsTasks.Count);
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
                ReprocessMessages.Add(LeadProviderDBRecordsTask.Item2.message);
                //TODO change error message if email discarded
                _logger.LogError("{tag} lead provider dbRecordsCreation and error {Error}", TagConstants.createDbRecordsResults, LeadProviderDBRecordsTask.Item1.Exception.Message + LeadProviderDBRecordsTask.Item1.Exception.StackTrace);
            }
            else
            {
                var Newlead = LeadProviderDBRecordsTask.Item1.Result.Lead;
                bool exists = false;
                //only relevant for admins
                //if (Newlead.BrokerId == null) exists = await localdbContext.LeadEmails.AnyAsync(e => e.EmailAddress == Newlead.LeadEmails.First().EmailAddress && e.Lead.AgencyId == brokerDTO.AgencyId);
                //relevant for brokers and admins who assigned lead to another broker or themselve
                //else exists = await localdbContext.LeadEmails.AnyAsync(e => e.EmailAddress == Newlead.LeadEmails.First().EmailAddress && e.Lead.BrokerId == Newlead.BrokerId);
                exists = await localdbContext.LeadEmails.AnyAsync(e => e.EmailAddress == Newlead.LeadEmails.First().EmailAddress && e.Lead.AgencyId == brokerDTO.AgencyId);
                if (!exists)
                {
                    localdbContext.Leads.Add(Newlead);
                    leadsAdded.Add(new Tuple<EmailparserDBRecrodsRes, GmailMessageDecoded>(LeadProviderDBRecordsTask.Item1.Result, LeadProviderDBRecordsTask.Item2));
                }
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
                ReprocessMessages.Add(UnknownDBRecordsTask.Item2.message);
                //TODO change error message if email discarded
                var errMessage = UnknownDBRecordsTask.Item1?.Exception?.Message ?? "null message";
                var stackTrace = UnknownDBRecordsTask.Item1?.Exception?.InnerException?.StackTrace ?? "null stackTrace";
                _logger.LogError("{tag} Unknown sender dbRecordsCreation for {messageId} and error {Error}", TagConstants.createDbRecordsResults, UnknownDBRecordsTask.Item2, errMessage + stackTrace);
            }
            else
            {
                var Newlead = UnknownDBRecordsTask.Item1.Result.Lead;
                bool exists = false;
                //only relevant for admins
                //if (Newlead.BrokerId == null) exists = await localdbContext.LeadEmails.AnyAsync(e => e.EmailAddress == Newlead.LeadEmails.First().EmailAddress && e.Lead.AgencyId == brokerDTO.AgencyId);
                //relevant for brokers and admins who assigned lead to another broker or themselve
                //else exists = await localdbContext.LeadEmails.AnyAsync(e => e.EmailAddress == Newlead.LeadEmails.First().EmailAddress && e.Lead.BrokerId == Newlead.BrokerId);
                exists = await localdbContext.LeadEmails.AnyAsync(e => e.EmailAddress == Newlead.LeadEmails.First().EmailAddress && e.Lead.AgencyId == brokerDTO.AgencyId);
                if (!exists)
                {
                    localdbContext.Leads.Add(Newlead);
                    leadsAdded.Add(new Tuple<EmailparserDBRecrodsRes, GmailMessageDecoded>(UnknownDBRecordsTask.Item1.Result, UnknownDBRecordsTask.Item2));
                }
            }
        }
        //transaction-------------------------------
        using var transaction = await localdbContext.Database.BeginTransactionAsync();

        //TODO add events/emails that are not attached to lead to the dbcontext

        //Dictionary<int, int> listingIdToNewLeadCount = new();
        //leadsAdded.Where(l => l.Item1.Lead.ListingId != null).GroupBy(l => l.Item1.Lead.ListingId).ToList().ForEach(g => listingIdToNewLeadCount.Add((int)g.Key, g.Count()));

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

        var ActionPlanEvents = new List<AppEvent>();
        //Stop action plans for leads that replied
        if (LeadIDsToStopActionPlan.Any())
        {
            var ActionPlanSuccesdict = new Dictionary<int, int>();//action plan id, times response
            foreach (var leadId in LeadIDsToStopActionPlan)
            {
                var ActionPlanAssociations = await localdbContext.ActionPlanAssociations
                    .Include(apa => apa.ActionPlan)
                    .Include(apa => apa.ActionTrackers.Where(a => a.ActionStatus == ActionStatus.ScheduledToStart || a.ActionStatus == ActionStatus.Failed))
                    .Where(apa => apa.LeadId == leadId && apa.ThisActionPlanStatus == ActionPlanStatus.Running && apa.ActionPlan.StopPlanOnInteraction)
                    .ToListAsync();
                //will probably always be 1 at the beginning
                foreach (var apass in ActionPlanAssociations)
                {
                    var APStopppedEvent = StopActionPlan(brokerDTO.Id, apass);
                    if (APStopppedEvent != null)
                    {
                        ActionPlanEvents.Add(APStopppedEvent);
                        if (ActionPlanSuccesdict.ContainsKey((int)apass.ActionPlanId))
                            ActionPlanSuccesdict[(int)apass.ActionPlanId]++;
                        else ActionPlanSuccesdict.Add((int)apass.ActionPlanId, 1);
                    }
                }
            }
            localdbContext.AppEvents.AddRange(ActionPlanEvents);
            foreach (var keyValuePair in ActionPlanSuccesdict)
            {
                var actionPId = keyValuePair.Key;
                var value = keyValuePair.Value;
                bool saved = false;
                byte count = 0;
                while (!saved && count <= 3)
                {
                    try
                    {
                        count++;
                        await _appDbContext.ActionPlans.Where(ap => ap.Id == actionPId)
                        .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(e => e.TimesSuccess, e => e.TimesSuccess + value));
                        saved = true;
                    }
                    catch (Exception ex)
                    {
                        await Task.Delay(300);
                    }
                }
            }
        }

        await localdbContext.SaveChangesAsync();

        //later maybe admin can define action plans that run on unassigned leads
        var assignedAddedLeads = leadsAdded.Where(l => l.Item1.Lead.BrokerId != null);
        //Trigger Action plans start
        int TimesActionPlanUsed = 0;
        if (assignedAddedLeads.Any())
        {
            //localdbContext.Leads.AddRange(leadsAdded.Select(l => l.Item1.Lead));            
            if (brokerDTO.brokerStartActionPlans.Any())
            {
                var actionPlan = brokerDTO.brokerStartActionPlans[0];
                foreach (var leadT in assignedAddedLeads)
                {
                    if (leadT.Item1.LeadEmailUnsure) continue;
                    var lead = leadT.Item1.Lead;
                    var LeadAssignmentEvent = lead.AppEvents.FirstOrDefault(e => e.EventType.HasFlag(EventType.LeadAssignedToYou));
                    if (LeadAssignmentEvent != null)
                    {
                        var added = TriggerActionPlan(actionPlan, lead, brokerDTO.Id);
                        localdbContext.Entry(lead).State = EntityState.Modified;
                        ActionPlanEvents.Add(added);
                        TimesActionPlanUsed++;
                        //has new ActionPlanAssociation
                        //and appEVent
                    }
                }
                await localdbContext.SaveChangesAsync();
            }
        }
        if (TimesActionPlanUsed > 0)
        {
            bool saved = false;
            byte count = 0;
            while (!saved && count <= 3)
            {
                try
                {
                    count++;
                    await _appDbContext.ActionPlans.Where(ap => ap.Id == brokerDTO.brokerStartActionPlans[0].Id)
                    .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(e => e.TimesUsed, e => e.TimesUsed + TimesActionPlanUsed));
                    saved = true;
                }
                catch (Exception ex)
                {
                    await Task.Delay(300);
                }
            }
        }

        var labelsRes = await _GmailService.Users.Labels.List("me").ExecuteAsync();
        var labels = labelsRes.Labels.ToList();
        var leadExtractedLabel = labels.FirstOrDefault(l => l.Name == "SealDeal:LeadCreated");
        if (leadExtractedLabel == null)
        {
            leadExtractedLabel = await _GmailService.Users.Labels.Create(new Label()
            {
                Name = "SealDeal:LeadCreated",
                LabelListVisibility = "labelShow",
                MessageListVisibility = "show"
            }, "me").ExecuteAsync();
            labels.Add(leadExtractedLabel);
        }

        //mark the messages that had a lead with "LeadExtracted"

        if (leadsAdded.Any())
        {
            try
            {
                await _GmailService.Users.Messages.BatchModify(new BatchModifyMessagesRequest
                {
                    AddLabelIds = new List<string>() { leadExtractedLabel.Id },
                    Ids = leadsAdded.Select(tup => tup.Item2.message.Id).ToList()
                }, "me").ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("{tag} adding 'leadExtracted' email category error: {Error}", TagConstants.emailCategory, ex.Message);
            }
        }
        //TODO if neabled, forward all emails from leads to assigned brokers


        //mark the messages that failed with tag ReprocessMessageId"
        await TagFailedMessagesGMAIL(ReprocessMessages, labels);

        await transaction.CommitAsync();
        //transaction-------------------------------
        var appevents = leadsAdded.SelectMany(tup => tup.Item1.Lead.AppEvents).ToList();
        appevents.AddRange(ActionPlanEvents);
        var emailevents = leadsAdded.SelectMany(tup => tup.Item1.Lead.EmailEvents).ToList();
        emailevents.AddRange(KnownLeadEmailEvents);
        await _realTimeNotif.SendRealTimeNotifsAsync(_logger, brokerDTO.Id, true, true, null, appevents, emailevents);
        return tokens;
    }

    public int HandleTaskResultGmail(Task<OpenAIResponse?> leadTask, GmailMessageDecoded message, List<Tuple<Task<EmailparserDBRecrodsRes>, GmailMessageDecoded>> DBRecordsTasks, bool FromLeadProvider, BrokerEmailProcessingDTO brokerDTO, List<GmailMessage> reprocessMessages)
    {
        if (leadTask.IsFaulted) //Task Error : this shouldnt happen as there is try catch block inside tasks
        {
            //TODO check error type to discard email if needed
            reprocessMessages.Add(message.message);
            //TODO change error message if email discarded
            _logger.LogError("{tag} task faulted and error {Error}", TagConstants.handleTaskResult, leadTask?.Exception?.Message + leadTask?.Exception?.StackTrace);
            return 0;
        }

        var result = leadTask.Result;
        if (result == null)
        {
            _logger.LogError("{tag} HandleTaskResult result is null. discarding email", "HandleTaskResult");
            return 0;
        }

        if (!result.Success)
        {
            _logger.LogError("{tag} HandleTaskResult adding to ReprocessMessages, open ai parsing did not succeed." +
                " email parsing props: message : '{errorMessage}' and  type: '{errorType}'.", "HandleTaskResult", result.ErrorMessage, result.ErrorType);
            //TODO check error type to discard email if needed
            reprocessMessages.Add(message.message);
        }
        else if (result.HasLead && result.content != null) //no error and has lead
        {
            DBRecordsTasks.Add(new Tuple<Task<EmailparserDBRecrodsRes>, GmailMessageDecoded>(FetchListingAndCreateDBRecordsAsync(result.content, FromLeadProvider, brokerDTO, null, message), message));
        }
        else if (result.HasLead && result.content == null)
        {
            //error its null
            _logger.LogError("{tag} HandleTaskResult has lead but result.content is null, open ai parsing did not succeed." +
                " for messageId {messageId}.", "HandleTaskResult", result.ProcessedMessageGMAIL.message.Id);
        }
        else
        {

            //discard email, no lead found
        }
        return result.EmailTokensUsed;
    }

    public async Task<EmailEvent?> CreateEmailEventKnownLeadGmail(GmailMessageDecoded gmailMessageDecoded, string brokerEmail, int leadId)
    {

        var messages = await _GmailService.Users.Threads.Get(brokerEmail, gmailMessageDecoded.message.ThreadId)
            .Configure(r =>
            {
                r.Format = UsersResource.ThreadsResource.GetRequest.FormatEnum.Metadata;
            })
            .ExecuteAsync();

        var messList = messages.Messages.ToList();
        var emailEvent = new EmailEvent
        {
            BrokerEmail = brokerEmail,
            Id = gmailMessageDecoded.message.Id,
            LeadParsedFromEmail = false,
            Seen = gmailMessageDecoded.isRead,
            TimeReceived = gmailMessageDecoded.timeReceivedUTC,
            LeadId = leadId
        };
        if (messList.Count == 1 && messList[0].Id == gmailMessageDecoded.message.Id) //no other messages in thread
        {
            emailEvent.ConversationId = gmailMessageDecoded.message.ThreadId;
            emailEvent.NeedsAction = true;
            emailEvent.RepliedTo = false;
        }
        else
        {
            if (gmailMessageDecoded.isRead) return null;
            emailEvent.NeedsAction = false;
        }
        return emailEvent;
    }

    public async Task<List<GmailMessage>> GetFailedMessagesGMAILAsync(string brokerEmail, CancellationToken cancellationToken)
    {
        var messagesPage = await _GmailService.Users.Messages.List("me")
            .Configure(r =>
            {
                r.Q = "label:SealDealReprocess";
                r.IncludeSpamTrash = false;
            })
            .ExecuteAsync(cancellationToken);

        if(messagesPage.Messages != null)
        {
            var gmailMessages = new List<GmailMessage>(messagesPage.Messages.Count);
            var batchRequest = new BatchRequest(_GmailService);

            messagesPage.Messages.ToList().ForEach(m =>
            {
                var getRequest = _GmailService.Users.Messages.Get("me", m.Id);
                getRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
                batchRequest.Queue<GmailMessage>(getRequest,
                 (content, error, i, message) =>
                 {
                     gmailMessages.Insert(i, content);
                 });
            });
            await batchRequest.ExecuteAsync();
            return gmailMessages;
        }
        return Enumerable.Empty<GmailMessage>().ToList();
        
    }

    public async Task TagFailedMessagesGMAIL(List<GmailMessage> gmailMAILReprocessMessages, List<Label> labels)
    {
        if (gmailMAILReprocessMessages.Count == 0) return;
        var reprocessLabel = labels.FirstOrDefault(l => l.Name == "SealDealReprocess");
        if (reprocessLabel == null)
        {
            reprocessLabel = await _GmailService.Users.Labels.Create(new Label
            {
                Name = "SealDealReprocess",
                LabelListVisibility = "labelHide",
                MessageListVisibility = "hide"
            }, "me").ExecuteAsync();
            labels.Add(reprocessLabel);
        }
        try
        {
            await _GmailService.Users.Messages.BatchModify(new BatchModifyMessagesRequest
            {
                AddLabelIds = new List<string>() { reprocessLabel.Id },
                Ids = gmailMAILReprocessMessages.Select(m => m.Id).ToList()
            }, "me").ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("{tag} adding 'leadExtracted' email category error: {Error}", TagConstants.emailCategory, ex.Message);
        }
    }
    // common not very important now 
    public AppEvent StopActionPlan(Guid brokerId, ActionPlanAssociation apass)
    {
        var APDoneEvent = new AppEvent
        {
            LeadId = apass.LeadId,
            BrokerId = brokerId,
            EventTimeStamp = DateTime.UtcNow,
            EventType = EventType.ActionPlanFinished,
            ReadByBroker = false,
            IsActionPlanResult = true,
            ProcessingStatus = ProcessingStatus.NoNeed
        };
        APDoneEvent.Props[NotificationJSONKeys.ActionPlanId] = apass.ActionPlanId.ToString();
        APDoneEvent.Props[NotificationJSONKeys.APFinishedReason] = NotificationJSONKeys.LeadResponded;
        APDoneEvent.Props[NotificationJSONKeys.ActionPlanName] = apass.ActionPlan.Name;

        apass.ThisActionPlanStatus = ActionPlanStatus.CancelledByLeadResponse;
        if (apass.ActionTrackers.Any())
        {
            foreach (var ta in apass.ActionTrackers)
            {
                ta.ActionStatus = ActionStatus.CancelledByLeadResponse;
                var jobId = ta.HangfireJobId;
                if (jobId != null)
                    try
                    {
                        BackgroundJob.Delete(jobId);
                    }
                    catch (Exception) { }
            }
        }
        return APDoneEvent;
    }

    public AppEvent TriggerActionPlan(ActionPlan actionPlan, Lead lead, Guid brokerId)
    {
        var timeNow = DateTime.UtcNow;
        var ap = actionPlan;

        var FirstActionDelay = ap.FirstActionDelay;
        var delays = FirstActionDelay?.Split(':');
        TimeSpan timespan = TimeSpan.Zero;
        if (delays != null)
        {
            if (int.TryParse(delays[0], out var days)) timespan += TimeSpan.FromDays(days);
            if (int.TryParse(delays[1], out var hours)) timespan += TimeSpan.FromHours(hours);
            if (int.TryParse(delays[2], out var minutes)) timespan += TimeSpan.FromMinutes(minutes);
        }
        var firstAction = ap.Actions[0];
        var actionTracker = new ActionTracker
        {
            TrackedActionId = firstAction.Id,
            ActionStatus = ActionStatus.ScheduledToStart,
            HangfireScheduledStartTime = timeNow + timespan,
        };
        var apAssociation = new ActionPlanAssociation
        {
            //LeadId = lead.Id,
            ActionPlanId = ap.Id,
            ActionPlanTriggeredAt = timeNow,
            ThisActionPlanStatus = ActionPlanStatus.Running,
            ActionTrackers = new() { actionTracker },
            currentTrackedActionId = firstAction.Id,
        };
        if (lead.ActionPlanAssociations == null) lead.ActionPlanAssociations = new();
        lead.ActionPlanAssociations.Add(apAssociation);

        bool OldHasActionPlanToStop = lead.HasActionPlanToStop;
        if (ap.StopPlanOnInteraction && !OldHasActionPlanToStop) lead.HasActionPlanToStop = true;
        if (ap.EventsToListenTo != EventType.None)
        {
            lead.EventsForActionPlans |= ap.EventsToListenTo; //for now not used
        }
        var APStartedEvent = new AppEvent
        {
            BrokerId = brokerId,
            EventTimeStamp = timeNow,
            EventType = EventType.ActionPlanStarted,
            IsActionPlanResult = true,
            ReadByBroker = false,
            ProcessingStatus = ProcessingStatus.NoNeed,
        };
        APStartedEvent.Props[NotificationJSONKeys.APTriggerType] = EventType.LeadAssignedToYou.ToString();
        APStartedEvent.Props[NotificationJSONKeys.ActionPlanId] = ap.Id.ToString();
        APStartedEvent.Props[NotificationJSONKeys.ActionPlanName] = ap.Name;
        lead.AppEvents.Add(APStartedEvent); //never null cuz this lead is newly created so it has that notif
        string HangfireJobId = "";
        try
        {
            if (delays != null)
            {
                HangfireJobId = BackgroundJob.Schedule<APProcessor>(p => p.DoActionAsync(lead.Id, firstAction.Id, firstAction.ActionLevel, ap.Id, null, CancellationToken.None), timespan);
            }
            else
            {
                HangfireJobId = BackgroundJob.Enqueue<APProcessor>(p => p.DoActionAsync(lead.Id, firstAction.Id, firstAction.ActionLevel, ap.Id, null, CancellationToken.None));
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical("{tag} Hangfire error scheduling ActionPlan processor from email processor/trigger action plan" +
             " for ActionPlan {actionPlanID} and Lead {leadID} with error {error}", TagConstants.HangfireScheduleActionPlan, ap.Id, lead.Id, ex.Message + ": " + ex.StackTrace);
            lead.ActionPlanAssociations.Remove(apAssociation);
            lead.AppEvents.Remove(APStartedEvent);
            lead.HasActionPlanToStop = OldHasActionPlanToStop;
        }
        actionTracker.HangfireJobId = HangfireJobId;

        return APStartedEvent;
    }

}
