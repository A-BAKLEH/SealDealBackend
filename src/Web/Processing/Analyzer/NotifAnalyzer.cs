using Core.Domain.NotificationAggregate;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Web.RealTimeNotifs;

namespace Web.Processing.Analyzer;

public class NotifAnalyzer
{
    private readonly ILogger<NotifAnalyzer> _logger;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ADGraphWrapper _aDGraphWrapper;
    public NotifAnalyzer(IDbContextFactory<AppDbContext> contextFactory, ADGraphWrapper aDGraphWrapper, ILogger<NotifAnalyzer> logger)
    {
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
    public async Task<Tuple<bool, bool>> CheckSeenAndRepliedToAsync(GraphServiceClient graphServiceClient, string brokerEmail, string messageId, string? convoId, bool Seen, bool Reply)
    {
        try
        {
            if (Seen && !Reply)
            {
                var mess = await graphServiceClient
                .Users[brokerEmail]
                .Messages[messageId]
                .GetAsync(config =>
                {
                    config.QueryParameters.Select = new string[] { "id", "isRead" };
                    config.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"" });
                });
                return new Tuple<bool, bool>((bool)mess.IsRead, false);
            }
            else
            {
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
                var messs = messages.Value;
                bool replied = messs.Count > 1 && messs.Any(m => m.From.EmailAddress.Address == brokerEmail);
                bool originalSeen = replied;
                if (!replied) originalSeen = (bool)messs.First(m => m.Id == messageId).IsRead;
                return new Tuple<bool, bool>(replied, replied);
            }
        }
        catch (Exception ex)
        {
            //TODO: log
            _logger.LogCritical("sdf {error}", ex.Message);
            return new Tuple<bool, bool>(true, true);
        }

    }
    public async Task AnalyzeNotifsAsync(Guid brokerId)
    {
        using var dbcontext = _contextFactory.CreateDbContext();
        var TimeNow = DateTimeOffset.UtcNow;
        /*
    1)   Any New lead events that are still unseen since > 15 mins, ordered by time since created, (priority 1)
	2)   Unseen emails from leads for 1 > hours (priority 2)
	3)   Seen && ReplyNeeded = true && Unreplied-to emails from leads for 2 > hours, ordered by lead by number of unreplied-to emails Descending (priority 3)
    4)   Other app events with notify Broker = true and Seen = false for > 1 days (priority 4)
	5)   for admin, get all unassigned created Leads that have been unassigned for 1 > hours (priority 1)
        */

        var broker = await dbcontext.Brokers
            .Include(b => b.ConnectedEmails)
            .FirstAsync(b => b.Id == brokerId);
        var notifs = new List<Notif>();

        var FstNotifyTrueAndUnseenEvent = await dbcontext.AppEvents
            .Select(e => new { e.Id, e.BrokerId, e.NotifyBroker, e.ReadByBroker })
            .OrderBy(e => e.Id)
            .FirstOrDefaultAsync(e => e.Id >= broker.LastSeenAppEventId && e.BrokerId == brokerId && e.NotifyBroker == true && e.ReadByBroker == false);
        var NewLastSeenAppEventId = FstNotifyTrueAndUnseenEvent?.Id ?? 0;

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

        var emailEvents = await dbcontext.EmailEvents
            .Where(e => e.BrokerId == brokerId && e.TimeReceived > broker.EmailEventAnalyzerLastTimestamp)
            .OrderBy(e => e.TimeReceived)
            .ToListAsync();

        //brokerEmail , GraphClient
        var dict = new Dictionary<string, GraphServiceClient>();
        var emails = emailEvents.DistinctBy(e => e.BrokerEmail).Select(e => e.BrokerEmail);
        foreach (var e in emails)
        {
            dict.Add(e, _aDGraphWrapper.CreateExtraClient(broker.ConnectedEmails.First(connemail => connemail.Email == e).tenantId));
        }

        if (emailEvents.Count > 0)
        {
            var newEmailEventAnalyzerLastTimestamp = broker.EmailEventAnalyzerLastTimestamp;
            var tasks = emailEvents.Select(async (e) =>
            {
                if (!e.Seen && e.TimeReceived <= TimeNow - TimeSpan.FromHours(1))
                {
                    newEmailEventAnalyzerLastTimestamp = e.TimeReceived;
                    if (!e.NeedsAction)
                    {
                        //just see if seen yet
                        var resT = await CheckSeenAndRepliedToAsync(dict[e.BrokerEmail], e.BrokerEmail, e.Id, null, true, false);
                        //not seen
                        if (!resT.Item1)
                        {
                            var notif = new Notif
                            {
                                BrokerId = brokerId,
                                LeadId = e.LeadId,
                                NotifType = EventType.UnSeenEmail,
                                CreatedTimeStamp = DateTimeOffset.UtcNow,
                                isSeen = false,
                                EventId = e.Id,
                                priority = 2
                            };
                            return notif;
                        };
                        return null;
                    }
                    else if (e.TimeReceived <= TimeNow - TimeSpan.FromHours(3))//not seen and needs action
                    {
                        //check if seen and replied-to yet
                        var resT = await CheckSeenAndRepliedToAsync(dict[e.BrokerEmail], e.BrokerEmail, e.Id, null, true, true);
                        e.Seen = resT.Item1;
                        e.RepliedTo = resT.Item2;
                        if (!resT.Item1 || !resT.Item2)
                        {
                            var notif = new Notif
                            {
                                BrokerId = brokerId,
                                LeadId = e.LeadId,
                                NotifType = !resT.Item1 ? EventType.UnSeenEmail : EventType.UnrepliedEmail,
                                CreatedTimeStamp = DateTimeOffset.UtcNow,
                                isSeen = false,
                                EventId = e.Id,
                                priority = 2
                            };
                            if (resT.Item1) e.TimesReplyNeededReminded++;
                            return notif;
                        }
                        return null;
                    }
                }
                else if (e.Seen && e.NeedsAction && !e.RepliedTo && e.TimeReceived <= TimeNow - TimeSpan.FromHours(3))
                {
                    newEmailEventAnalyzerLastTimestamp = e.TimeReceived;
                    //check if replied-to yet
                    var resT = await CheckSeenAndRepliedToAsync(dict[e.BrokerEmail], e.BrokerEmail, e.Id, null, false, true);
                    e.RepliedTo = resT.Item2;
                    if (!resT.Item2)
                    {
                        var notif = new Notif
                        {
                            BrokerId = brokerId,
                            LeadId = e.LeadId,
                            NotifType = EventType.UnrepliedEmail,
                            CreatedTimeStamp = DateTimeOffset.UtcNow,
                            isSeen = false,
                            EventId = e.Id,
                            priority = 2
                        };
                        e.TimesReplyNeededReminded++;
                        return notif;
                    }
                    return null;
                }
                return null;
            });

            await Task.WhenAll(tasks);
            notifs.AddRange(tasks.Where(t => t.Result != null).Select(t => t.Result));

            broker.EmailEventAnalyzerLastTimestamp = newEmailEventAnalyzerLastTimestamp;
        }

        var StillUnRepliedEmailEvents = await dbcontext.EmailEvents
            .Where(e => e.BrokerId == brokerId && e.Seen && e.NeedsAction && !e.RepliedTo && e.TimeReceived < broker.EmailEventAnalyzerLastTimestamp)
            .OrderBy(e => e.TimeReceived)
            .ToListAsync();
        emails = StillUnRepliedEmailEvents.DistinctBy(e => e.BrokerEmail).Select(e => e.BrokerEmail);
        foreach (var e in emails)
        {
            if (!dict.ContainsKey(e)) dict.Add(e, _aDGraphWrapper.CreateExtraClient(broker.ConnectedEmails.First(connemail => connemail.Email == e).tenantId));
        }
        if (StillUnRepliedEmailEvents.Count > 0)
        {
            foreach (var unrepliedEmail in StillUnRepliedEmailEvents)
            {
                //check replied-to in graph
                var resT = await CheckSeenAndRepliedToAsync(dict[unrepliedEmail.BrokerEmail], unrepliedEmail.BrokerEmail, unrepliedEmail.Id, unrepliedEmail.ConversationId, false, true);
                var existingNotif = await dbcontext.Notifs.FirstOrDefaultAsync(n => n.BrokerId == brokerId && n.LeadId == unrepliedEmail.LeadId && !n.isSeen && n.NotifType == EventType.UnrepliedEmail && n.EventId == unrepliedEmail.Id);

                //replied to
                if (resT.Item2)
                {
                    unrepliedEmail.RepliedTo = true;
                    if (existingNotif != null) existingNotif.isSeen = true;
                }
                //still unreplied to
                else if (existingNotif != null)
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
                            CreatedTimeStamp = DateTimeOffset.UtcNow,
                            isSeen = false,
                            EventId = unrepliedEmail.Id,
                            priority = 2
                        });
                        unrepliedEmail.TimesReplyNeededReminded++;
                    }
                }
            }
        }
        //----------------------
        var appeventFilterBiggerThanID = 0;
        if (broker.AppEventAnalyzerLastId > NewLastSeenAppEventId || broker.AppEventAnalyzerLastId == NewLastSeenAppEventId) appeventFilterBiggerThanID = broker.AppEventAnalyzerLastId;
        else appeventFilterBiggerThanID = NewLastSeenAppEventId - 1;
        if (appeventFilterBiggerThanID < 0) appeventFilterBiggerThanID = 0;

        int leadAssignedToYou = (int)EventType.LeadAssignedToYou;
        var UnseenLeadAssignedEventsToAnalyze = await dbcontext.AppEvents
        .Where(x => x.BrokerId == brokerId && x.Id > appeventFilterBiggerThanID && x.NotifyBroker == true && x.ReadByBroker == false &&
        (((int)x.EventType & leadAssignedToYou) > 0))
        .OrderBy(x => x.Id)
        .AsNoTracking()
        .ToListAsync();

        var currHighestAnalyzedAppEventId = 0;
        UnseenLeadAssignedEventsToAnalyze.ForEach(e =>
            {
                //unseen LeadAssigned events > 15 mins
                if (e.EventTimeStamp <= TimeNow - TimeSpan.FromMinutes(15))
                {
                    currHighestAnalyzedAppEventId = e.Id;
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
            broker.LastUnassignedLeadIdAnalyzed = unassignedCreatedLeads.Last().Id;
        }

        broker.LastSeenAppEventId = NewLastSeenAppEventId;
        broker.AppEventAnalyzerLastId = currHighestAnalyzedAppEventId;
        dbcontext.Notifs.AddRange(notifs);
        await dbcontext.SaveChangesAsync();
        //TODO real time notifs, check that email parsing running correctly
        await RealTimeNotifSender.SendRealTimeNotifsAsync(_logger,brokerId,true,true, notifs, null,null);
    }
}
