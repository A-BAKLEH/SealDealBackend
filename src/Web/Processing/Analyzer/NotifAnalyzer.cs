using Core.Config.Constants.LoggingConstants;
using Core.Domain.NotificationAggregate;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Requests;
using Google.Apis.Services;
using Hangfire.Server;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Serilog.Context;
using Web.Processing.EmailAutomation;
using Web.RealTimeNotifs;
using EventType = Core.Domain.NotificationAggregate.EventType;
namespace Web.Processing.Analyzer;

public class NotifAnalyzer
{
    private readonly ILogger<NotifAnalyzer> _logger;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ADGraphWrapper _aDGraphWrapper;
    private readonly RealTimeNotifSender _realTimeNotif;
    public NotifAnalyzer(IDbContextFactory<AppDbContext> contextFactory, RealTimeNotifSender realTimeNotifSender, ADGraphWrapper aDGraphWrapper, ILogger<NotifAnalyzer> logger)
    {
        _realTimeNotif = realTimeNotifSender;
        _logger = logger;
        _contextFactory = contextFactory;
        _aDGraphWrapper = aDGraphWrapper;
    }

    /// <summary>
    /// returns seen,replied-to
    /// </summary>
    /// <param name="graphServiceClient"></param>
    /// <param name="brokerEmail"></param>
    /// <param name="messageId"></param>
    /// <param name="convoId"></param>
    /// <param name="Seen"></param>
    /// <param name="Reply"></param>
    /// <returns></returns>
    public async Task<Tuple<bool, bool>> CheckSeenAndRepliedToAsync(GraphServiceClient? graphServiceClient, GmailService? _GmailService, string brokerEmail, string messageId, string? convoId, bool Seen, bool Reply)
    {
        bool isMsft = graphServiceClient != null;
        try
        {
            if (Seen && !Reply)
            {
                if (isMsft)
                {
                    var mess = await graphServiceClient
                    .Users[brokerEmail]
                    .Messages[messageId]
                    .GetAsync(config =>
                    {
                        config.QueryParameters.Select = new string[] { "id", "isRead" };
                        config.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"" });
                    });
                    if (mess == null) return new Tuple<bool, bool>(true, true); //for deleted messages
                    return new Tuple<bool, bool>((bool)mess.IsRead, false);
                }
                else
                {
                    var mess = await _GmailService.Users.Messages.Get(brokerEmail, messageId)
                        .Configure(a => a.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata).ExecuteAsync();
                    if (mess == null) return new Tuple<bool, bool>(true, true); //for deleted messages
                    return new Tuple<bool, bool>(!mess.LabelIds.Contains("UNREAD"), false);
                }
            }
            else
            {
                if (isMsft)
                {
                    if (convoId == null)
                    {
                        _logger.LogCritical("{tag} verify seen replied to convoId is null for message {messageId} and broker email {email}", TagConstants.checkSeenAndRepliedTo, messageId, brokerEmail);
                        return new Tuple<bool, bool>(true, true);
                    }
                    var date1 = DateTimeOffset.UtcNow - TimeSpan.FromDays(200);
                    var date = date1.ToString("o");
                    var messages = await graphServiceClient
                      .Users[brokerEmail]
                      .Messages
                      .GetAsync(config =>
                      {
                          config.QueryParameters.Top = 5;
                          config.QueryParameters.Select = new string[] { "id", "from", "conversationId", "isRead", "toRecipients", "receivedDateTime" };
                          config.QueryParameters.Filter = $"receivedDateTime gt {date} and conversationId eq '{convoId}'";
                          config.QueryParameters.Orderby = new string[] { "receivedDateTime" };
                          config.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"" });
                      });
                    //for deleted messages
                    if (messages == null || messages.Value == null || messages.Value.Count == 0) return new Tuple<bool, bool>(true, true);
                    var messs = messages.Value;
                    bool replied = messs.Count > 1 && messs.Any(m => m.From.EmailAddress.Address == brokerEmail);
                    bool? originalSeen = messs.FirstOrDefault(m => m.Id == messageId)?.IsRead;
                    bool finalSeen = originalSeen ?? replied;
                    return new Tuple<bool, bool>(finalSeen, replied);
                }
                else
                {
                    var messages = await _GmailService.Users.Threads.Get(brokerEmail, convoId)
                        .Configure(r =>
                        {
                            r.Format = UsersResource.ThreadsResource.GetRequest.FormatEnum.Metadata;
                        })
                        .ExecuteAsync();
                    //for deleted messages
                    if (messages == null || messages.Messages == null || messages.Messages.Count == 0) return new Tuple<bool, bool>(true, true);
                    var messs = messages.Messages.ToList();
                    bool replied = messs.Count > 1 && messs.Any(m => EmailProcessor.ConvertGmailHeaderFieldToPeople(m.Payload.Headers.FirstOrDefault(h => h.Name == "From")?.Value).First().Address == brokerEmail);
                    bool? originalSeen = messs.FirstOrDefault(m => m.Id == messageId)?.LabelIds.Contains("UNREAD");
                    if (originalSeen != null) originalSeen = !originalSeen;
                    bool finalSeen = originalSeen ?? replied;
                    return new Tuple<bool, bool>(finalSeen, replied);
                }
            }
        }
        catch (ODataError er)
        {
            var error = er.Error;
            _logger.LogCritical("{tag} problem verifying seen replied to {error}", TagConstants.checkSeenAndRepliedTo, error.Code + " :" + error.Message);
            return new Tuple<bool, bool>(true, true);
        }
        catch (Exception ex)
        {
            var error = ex;
            _logger.LogCritical("{tag} problem verifying seen replied to {error}", TagConstants.checkSeenAndRepliedTo, error.Message);
            return new Tuple<bool, bool>(true, true);
        }

    }

    public async Task<Tuple<Notif?, DateTime?>?> AnalyzeEmail(EmailEvent e, DateTime TimeNow, GraphServiceClient? graphServiceClient, GmailService? gmailService)
    {
        DateTime? newEmailEventAnalyzerLastTimestamp = null;
        if (!e.Seen && e.TimeReceived <= TimeNow - TimeSpan.FromHours(1))
        //if (!e.Seen && e.TimeReceived <= TimeNow - TimeSpan.FromSeconds(1))
        {
            newEmailEventAnalyzerLastTimestamp = e.TimeReceived;
            if (!e.NeedsAction)
            {
                //just see if seen yet
                var resT = await CheckSeenAndRepliedToAsync(graphServiceClient, gmailService, e.BrokerEmail, e.Id, null, true, false);
                if (resT == null)
                {
                    return null;
                }
                //not seen
                if (!resT.Item1)
                {
                    var notif = new Notif
                    {
                        BrokerId = e.BrokerId,
                        LeadId = e.LeadId,
                        NotifType = EventType.UnSeenEmail,
                        CreatedTimeStamp = DateTime.UtcNow,
                        isSeen = false,
                        EventId = e.Id,
                        priority = 2
                    };
                    return new Tuple<Notif?, DateTime?>(notif, newEmailEventAnalyzerLastTimestamp);
                }
                //seen so mark email event as seen
                else
                {
                    e.Seen = true;
                    return new Tuple<Notif?, DateTime?>(null, newEmailEventAnalyzerLastTimestamp);
                }

            }
            else if (e.TimeReceived <= TimeNow - TimeSpan.FromHours(3))//not seen and needs action
            //else if (e.TimeReceived <= TimeNow - TimeSpan.FromSeconds(1))//not seen and needs action
            {
                //check if seen and replied-to yet
                var resT = await CheckSeenAndRepliedToAsync(graphServiceClient, gmailService, e.BrokerEmail, e.Id, e.ConversationId, true, true);
                if (resT == null)
                {
                    return null;
                }
                e.Seen = resT.Item1;
                e.RepliedTo = resT.Item2;
                if (!resT.Item1 || !resT.Item2)
                {
                    var notif = new Notif
                    {
                        BrokerId = e.BrokerId,
                        LeadId = e.LeadId,
                        NotifType = !resT.Item1 ? EventType.UnSeenEmail : EventType.UnrepliedEmail,
                        CreatedTimeStamp = DateTime.UtcNow,
                        isSeen = false,
                        EventId = e.Id,
                        priority = 2
                    };
                    if (resT.Item1) e.TimesReplyNeededReminded++;//means notif type is it wasnt replied to
                    return new Tuple<Notif?, DateTime?>(notif, newEmailEventAnalyzerLastTimestamp);
                }
                return new Tuple<Notif?, DateTime?>(null, newEmailEventAnalyzerLastTimestamp);
            }
        }
        else if (e.Seen && e.NeedsAction && !e.RepliedTo && e.TimeReceived <= TimeNow - TimeSpan.FromHours(3))
        //else if (e.Seen && e.NeedsAction && !e.RepliedTo && e.TimeReceived <= TimeNow - TimeSpan.FromSeconds(1))
        {
            newEmailEventAnalyzerLastTimestamp = e.TimeReceived;
            //check if replied-to yet
            var resT = await CheckSeenAndRepliedToAsync(graphServiceClient, gmailService, e.BrokerEmail, e.Id, e.ConversationId, false, true);
            if (resT == null)
            {
                return null;
            }
            e.RepliedTo = resT.Item2;
            if (!resT.Item2)
            {
                var notif = new Notif
                {
                    BrokerId = e.BrokerId,
                    LeadId = e.LeadId,
                    NotifType = EventType.UnrepliedEmail,
                    CreatedTimeStamp = DateTime.UtcNow,
                    isSeen = false,
                    EventId = e.Id,
                    priority = 2
                };
                e.TimesReplyNeededReminded++;
                return new Tuple<Notif?, DateTime?>(notif, newEmailEventAnalyzerLastTimestamp);
            }
            return new Tuple<Notif?, DateTime?>(null, newEmailEventAnalyzerLastTimestamp);
        }
        return new Tuple<Notif?, DateTime?>(null, newEmailEventAnalyzerLastTimestamp);
    }
    public async Task AnalyzeNotifsAsync(Guid brokerId, PerformContext performContext, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("hanfireJobId", performContext.BackgroundJob.Id))
        using (LogContext.PushProperty("UserId", brokerId.ToString()))
        {
            try
            {
                using var dbcontext = _contextFactory.CreateDbContext();
                var TimeNow = DateTime.UtcNow;
                /*
            1)   Any New lead events that are still unseen since > 15 mins, ordered by time since created, (priority 1)
            2)   Unseen emails from leads for 1 > hours (priority 2)
            3)   Seen && ReplyNeeded = true && Unreplied-to emails from leads for 2 > hours, ordered by lead by number of unreplied-to emails Descending (priority 3)
            4)   Other app events with notify Broker = true and Seen = false for > 1 days (priority 4)
            5)   for admin, get all unassigned created Leads that have been unassigned for 1 > hours (priority 1)
                */

                var broker = await dbcontext.Brokers
                    .Include(b => b.ConnectedEmails)
                    .FirstOrDefaultAsync(b => b.Id == brokerId);
                if (broker == null)
                {
                    _logger.LogWarning("{tag} broker {brokerId} does not exist", "analyzer", brokerId);
                    return;
                }
                if (broker.EmailEventAnalyzerLastTimestamp < broker.Created)
                {
                    broker.EmailEventAnalyzerLastTimestamp = broker.Created;
                }
                var notifs = new List<Notif>();

                var FstNotifyTrueAndUnseenEvent = await dbcontext.AppEvents
                    .Select(e => new { e.Id, e.BrokerId, e.NotifyBroker, e.ReadByBroker })
                    .OrderBy(e => e.Id)
                    .FirstOrDefaultAsync(e => e.Id > broker.AppEventAnalyzerLastId && e.BrokerId == brokerId && e.NotifyBroker == true && e.ReadByBroker == false);
                int? FstNotifyTrueAndUnseenEventId = FstNotifyTrueAndUnseenEvent?.Id;
                /*
                2)   Unseen emails from leads check graph API to verify they still unseen , notify if
                unseen for 1 > hours (priority 2)

                3) Seen && ReplyNeeded = true && Unreplied - to emails from leads for 3 > hours,
                ordered by lead by number of unreplied - to emails Descending(priority 3)
                */
                //check if unseen emails are still unseen, and those replyNeeded if they were replied to or
                //if other emails were exchanged(and later calls / sms) between.
                //these relate to emails that are before broker.EmailEventAnalyzerLastTimestamp

                //var Emailfilter = (int)(EventType.UnSeenEmail | EventType.UnrepliedEmail);
                //var existingEmailNotifs = await dbcontext.Notifs
                //    .Where(n => n.BrokerId == brokerId && n.isSeen == false && (((int)n.NotifType & Emailfilter) > 0))
                //    .ToListAsync();


                //TODO We Assume only 1 connected email for now
                var brokerConnecteEmail = broker.ConnectedEmails.FirstOrDefault();
                if (brokerConnecteEmail == null) goto AfterEmailsLabel;

                var emailEvents = await dbcontext.EmailEvents
                    .Where(e => e.BrokerId == brokerId && e.TimeReceived > broker.EmailEventAnalyzerLastTimestamp)
                    .OrderBy(e => e.TimeReceived)
                    .ToListAsync();

                //TODO remove this when moving from 1 connected Email
                emailEvents = emailEvents.Where(e => e.BrokerEmail == brokerConnecteEmail.Email).ToList();

                //these are broker emails, we know at least one broker's connectedEmail exists here
                var emails = emailEvents.DistinctBy(e => e.BrokerEmail).Select(e => e.BrokerEmail)
                    .Where(e => e == brokerConnecteEmail.Email); //TODO remove this when 1+ emails

                //brokerEmail , GraphClient
                var dict = new Dictionary<string, GraphServiceClient>();
                GmailService? _GmailService = null;
                bool isMsft = brokerConnecteEmail.isMSFT;
                if (isMsft)
                {
                    foreach (var e in emails)
                    {
                        dict.Add(e, _aDGraphWrapper.CreateExtraClient(broker.ConnectedEmails.First(connemail => connemail.Email == e).tenantId));
                    }
                }
                else
                {
                    GoogleCredential cred = GoogleCredential.FromAccessToken(brokerConnecteEmail.AccessToken);
                    _GmailService = new GmailService(new BaseClientService.Initializer { HttpClientInitializer = cred });
                }

                if (emailEvents.Count > 0)
                {
                    DateTime? NewEmailEventAnalyzerLastTimestamp = null;
                    foreach (var e in emailEvents)
                    {
                        var res = await AnalyzeEmail(e, TimeNow, isMsft ? dict[e.BrokerEmail] : null, isMsft ? null : _GmailService);
                        if (res == null)
                        {
                            dbcontext.Remove(e);
                            continue;
                        }
                        if (res.Item1 != null) notifs.Add(res.Item1);
                        if (res.Item2 != null) NewEmailEventAnalyzerLastTimestamp = res.Item2;
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                    if (NewEmailEventAnalyzerLastTimestamp != null) broker.EmailEventAnalyzerLastTimestamp = (DateTime)NewEmailEventAnalyzerLastTimestamp;
                }

                //still unseen too
                var timeToInclude = broker.EmailEventAnalyzerLastTimestamp + TimeSpan.FromSeconds(1);
                var StillUnRepliedEmailEvents = await dbcontext.EmailEvents
                    .Where(e => e.BrokerId == brokerId && (!e.Seen || (e.Seen && e.NeedsAction && !e.RepliedTo)) && e.TimeReceived < timeToInclude)
                    .OrderBy(e => e.TimeReceived)
                    .ToListAsync();

                //TODO remove this when 1+ emails
                StillUnRepliedEmailEvents = StillUnRepliedEmailEvents.Where(e => e.BrokerEmail == brokerConnecteEmail.Email).ToList();
                if (StillUnRepliedEmailEvents == null || !StillUnRepliedEmailEvents.Any()) goto AfterEmailsLabel;

                emails = StillUnRepliedEmailEvents.DistinctBy(e => e.BrokerEmail).Select(e => e.BrokerEmail);
                if (isMsft)
                {
                    foreach (var e in emails)
                    {
                        if (!dict.ContainsKey(e)) dict.Add(e, _aDGraphWrapper.CreateExtraClient(broker.ConnectedEmails.First(connemail => connemail.Email == e).tenantId));
                    }
                }

                if (StillUnRepliedEmailEvents.Count > 0)
                {
                    foreach (var unrepliedEmail in StillUnRepliedEmailEvents)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        Tuple<bool, bool>? resT = null;
                        var seen = unrepliedEmail.Seen;
                        var needsAction = unrepliedEmail.NeedsAction;
                        var repliedTo = unrepliedEmail.RepliedTo;

                        if (!seen) // unseen
                        {
                            resT = await CheckSeenAndRepliedToAsync(isMsft ? dict[unrepliedEmail.BrokerEmail] : null, isMsft ? null : _GmailService,
                                unrepliedEmail.BrokerEmail, unrepliedEmail.Id, unrepliedEmail.ConversationId, true, false);
                            var existingNotifs = await dbcontext.Notifs
                                .Where(n => n.BrokerId == brokerId && n.LeadId == unrepliedEmail.LeadId && !n.isSeen && n.NotifType == EventType.UnSeenEmail && n.EventId == unrepliedEmail.Id)
                                .ToListAsync();
                            //if seen
                            if (resT.Item1)
                            {
                                unrepliedEmail.Seen = true;
                                if (existingNotifs != null)
                                {
                                    foreach (var existingNotif in existingNotifs)
                                    {
                                        existingNotif.isSeen = true;
                                    }
                                }
                            }
                            else if(!existingNotifs.Any()) //not seen, might or might not need action, only create UnSeen notif if there is no notif Unseen now
                            {
                                notifs.Add(
                                    new Notif
                                    {
                                        BrokerId = unrepliedEmail.BrokerId,
                                        LeadId = unrepliedEmail.LeadId,
                                        NotifType = EventType.UnSeenEmail,
                                        CreatedTimeStamp = DateTime.UtcNow,
                                        isSeen = false,
                                        EventId = unrepliedEmail.Id,
                                        priority = 2
                                    });
                            }
                        }
                        else //seen and needs Action and not replied to
                        {
                            resT = await CheckSeenAndRepliedToAsync(isMsft ? dict[unrepliedEmail.BrokerEmail] : null, isMsft ? null : _GmailService, unrepliedEmail.BrokerEmail, unrepliedEmail.Id, unrepliedEmail.ConversationId, false, true);
                            var existingNotif = await dbcontext.Notifs.FirstOrDefaultAsync(n => n.BrokerId == brokerId && n.LeadId == unrepliedEmail.LeadId && !n.isSeen && n.NotifType == EventType.UnrepliedEmail && n.EventId == unrepliedEmail.Id);
                            //replied to
                            if (resT.Item2)
                            {
                                unrepliedEmail.RepliedTo = true;
                                if (existingNotif != null) existingNotif.isSeen = true;
                            }
                            //still unreplied to and no notif to display for it
                            else if (!resT.Item2 && existingNotif == null)
                            {
                                if (unrepliedEmail.TimesReplyNeededReminded >= 2)
                                {
                                    unrepliedEmail.RepliedTo = true;
                                }
                                else
                                {
                                    notifs.Add(new Notif
                                    {
                                        BrokerId = brokerId,
                                        LeadId = unrepliedEmail.LeadId,
                                        NotifType = EventType.UnrepliedEmail,
                                        CreatedTimeStamp = DateTime.UtcNow,
                                        isSeen = false,
                                        EventId = unrepliedEmail.Id,
                                        priority = 2
                                    });
                                    unrepliedEmail.TimesReplyNeededReminded++;
                                }
                            }
                        }
                    }
                }

            AfterEmailsLabel:

                //----------------------
                var appeventFilterBiggerThanID = 0;
                if (FstNotifyTrueAndUnseenEventId != null) appeventFilterBiggerThanID = (int)FstNotifyTrueAndUnseenEventId - 1;
                else appeventFilterBiggerThanID = broker.AppEventAnalyzerLastId;
                if (appeventFilterBiggerThanID < 0) appeventFilterBiggerThanID = 0; //probably never necessary
                broker.AppEventAnalyzerLastId = appeventFilterBiggerThanID; //its updated to FstNotifyTrueAndUnseenEventId or stays the same

                int leadAssignedToYou = (int)EventType.LeadAssignedToYou;
                var UnseenLeadAssignedEventsToAnalyze = await dbcontext.AppEvents
                .Where(x => x.BrokerId == brokerId && x.Id > appeventFilterBiggerThanID && x.NotifyBroker == true && x.ReadByBroker == false &&
                (((int)x.EventType & leadAssignedToYou) > 0))
                .OrderBy(x => x.Id)
                .AsNoTracking()
                .ToListAsync();

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                UnseenLeadAssignedEventsToAnalyze.ForEach(e =>
                {
                    //unseen LeadAssigned events > 15 mins
                    if (e.EventTimeStamp <= TimeNow - TimeSpan.FromMinutes(15))
                    //if (e.EventTimeStamp <= TimeNow - TimeSpan.FromSeconds(1))
                    {
                        broker.AppEventAnalyzerLastId = e.Id; //updated every time cuz they ordered ASC
                        notifs.Add(new Notif
                        {
                            BrokerId = brokerId,
                            LeadId = e.LeadId,
                            CreatedTimeStamp = TimeNow,
                            NotifType = EventType.UnseenNewLead,
                            priority = 1,
                        });
                    }
                    //Other app events with notify Broker = true and Seen = false for > 1 days (priority 4)
                    //else if (!e.EventType.HasFlag(EventType.LeadCreated) && e.EventTimeStamp <= TimeNow - TimeSpan.FromDays(1))
                    //{
                    //    currHighestAnalyzedAppEventId = e.Id;
                    //    notifs.Add(new Notif
                    //    {
                    //        BrokerId = brokerId,
                    //        LeadId = e.LeadId,
                    //        CreatedTimeStamp = TimeNow,
                    //        NotifType = e.EventType,
                    //        priority = 4,
                    //    });
                    //}
                });

                //if admin get all unassigned created Leads that have been unassigned for 1 > hours (priority 1)
                var NowMinusOneHour = TimeNow - TimeSpan.FromHours(1);
                //var NowMinusOneHour = TimeNow - TimeSpan.FromSeconds(1);
                if (broker.isAdmin && !broker.isSolo)
                {
                    var unassignedCreatedLeads = await dbcontext.Leads
                        .Where(x => x.AgencyId == broker.AgencyId && x.Id >= broker.LastUnassignedLeadIdAnalyzed && x.BrokerId == null && x.EntryDate <= NowMinusOneHour)
                        .OrderBy(x => x.Id)
                        .AsNoTracking()
                        .ToListAsync();
                    unassignedCreatedLeads.ForEach(l => notifs.Add(new Notif
                    {
                        BrokerId = brokerId,
                        LeadId = l.Id,
                        CreatedTimeStamp = TimeNow,
                        NotifType = EventType.UnAssignedLead,
                        priority = 1,
                    }));
                    if (unassignedCreatedLeads.Any())
                        broker.LastUnassignedLeadIdAnalyzed = unassignedCreatedLeads.Last().Id;
                }

                dbcontext.Notifs.AddRange(notifs);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                await dbcontext.SaveChangesAsync();
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                //TODO real time notifs, check that email parsing running correctly
                await _realTimeNotif.SendRealTimeNotifsAsync(_logger, brokerId, true, true, notifs, null, null);
            }
            catch (ODataError err)
            {
                _logger.LogError("{tag} odata error : {error}", "NotifAnalyzer", err.Error.Message + " : " + err.Error.Code);
            }
            catch (Exception ex)
            {
                _logger.LogError("{tag} exception error : {error}", "NotifAnalyzer", ex.Message);
            }
        }
    }
}
