using Core.DTOs.NotifsDTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Web.Config.EnumExtens;
using Web.ControllerServices.StaticMethods;
using EventType = Core.Domain.NotificationAggregate.EventType;

namespace Web.ControllerServices.QuickServices;
public class NotificationService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    public NotificationService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    public async Task MarkLeadNotifsRead(int LeadId, Guid brokerId)
    {
        var timeNow = DateTime.UtcNow;
        using var AppEventsContext = _contextFactory.CreateDbContext();
        using var EmailEventsContext = _contextFactory.CreateDbContext();
        using var NotifContext = _contextFactory.CreateDbContext();

        var task1 = AppEventsContext.AppEvents
            .Where(e => e.LeadId == LeadId && e.BrokerId == brokerId && !e.ReadByBroker && e.EventTimeStamp < timeNow)
                .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.ReadByBroker, true));

        var task2 = EmailEventsContext.EmailEvents
            .Where(e => e.LeadId == LeadId && e.BrokerId == brokerId && !e.Seen && e.TimeReceived < timeNow)
                .ExecuteUpdateAsync(setters => setters
                               .SetProperty(e => e.Seen, true));
        var task3 = NotifContext.Notifs
            .Where(n => n.LeadId == LeadId && n.BrokerId == brokerId && !n.isSeen && n.CreatedTimeStamp < timeNow)
                .ExecuteUpdateAsync(setters => setters
                                              .SetProperty(n => n.isSeen, true));
        await task1;
        await task2;
        await task3;
        await AppEventsContext.Leads.Where(l => l.Id == LeadId && l.BrokerId == brokerId)
            .ExecuteUpdateAsync(setters => setters
                           .SetProperty(l => l.LastNotifsViewedAt, timeNow));
    }
    public async Task<DashboardPerLeadDTO> GetPerLeadNewNotifs(Guid brokerId, int LeadId, bool NormalTable, bool PriorityTable)
    {
        using var AppEventsContext = _contextFactory.CreateDbContext();
        using var EmailEventsContext = _contextFactory.CreateDbContext();
        using var NotifContext = _contextFactory.CreateDbContext();

        var brokerTask = AppEventsContext.Brokers
            .Select(b => new { b.Id, b.LastSeenAppEventId, b.isAdmin, b.isSolo,b.TimeZoneId })
            .FirstAsync(b => b.Id == brokerId);

        var leadTask = AppEventsContext.Leads
            .Select(l => new LeadForNotifsDTO
            {
                brokerId = l.BrokerId,
                LeadId = l.Id,
                LeadfirstName = l.LeadFirstName,
                LeadLastName = l.LeadLastName,
                LeadPhone = l.PhoneNumber,
                LeadEmail = l.LeadEmails.FirstOrDefault(e => e.IsMain).EmailAddress,
                LeadStatus = l.LeadStatus.ToString(),
                LastTimeYouViewedLead = l.LastNotifsViewedAt
            })
            .FirstAsync(l => l.LeadId == LeadId);


        var broker = await brokerTask;
        var lead = await leadTask;

        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(broker.TimeZoneId);
        var todaydate = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, timeZoneInfo).Date;
        var todayStart = todaydate.AddMinutes(0);
        var UTCstartDay = MyTimeZoneConverter.ConvertToUTC(timeZoneInfo, todayStart);

        var NormalTableFlags = EventType.LeadAssignedToYou | EventType.LeadStatusChange | EventType.ActionPlanFinished |  EventType.ActionPlanStarted;
        if (broker.isAdmin && !broker.isSolo)
        {
            NormalTableFlags |= EventType.LeadCreated | EventType.YouAssignedtoBroker;
        }

        var DashboardPerLeadDTO = new DashboardPerLeadDTO
        {
            LeadId = lead.LeadId,
            LeadUnAssigned = lead.brokerId == null,
            LeadfirstName = lead.LeadfirstName,
            LeadLastName = lead.LeadLastName,
            LeadPhone = lead.LeadPhone,
            LeadEmail = lead.LeadEmail,
            LeadStatus = lead.LeadStatus,
            LastTimeYouViewedLead = broker.isAdmin ? null : lead.LastTimeYouViewedLead,
        };
        if (NormalTable)
        {
            var AppEventsTask = AppEventsContext.AppEvents
            .Where(e => e.BrokerId == brokerId && e.LeadId == LeadId && e.NotifyBroker && (!e.ReadByBroker || e.EventTimeStamp >= UTCstartDay))
            .Select(e => new { e.Id, e.LeadId, e.EventTimeStamp, e.EventType,e.ReadByBroker })
            .OrderBy(e => e.ReadByBroker)
            .ThenByDescending(e => e.EventTimeStamp)
            .ToListAsync();

            var EmailEventsTask = EmailEventsContext.EmailEvents
                .Where(e => e.BrokerId == brokerId && e.LeadId == LeadId)
                .Select(e => new { e.Id, e.LeadId, e.BrokerEmail, e.Seen, e.NeedsAction, e.RepliedTo, e.TimeReceived })
                .OrderBy(e => e.Seen)
                .ThenByDescending(e => e.TimeReceived)
                .ToListAsync();

            var AppEvents = await AppEventsTask;
            var EmailEvents = await EmailEventsTask;

            DashboardPerLeadDTO.AppEvents = AppEvents
                .Where(e => NormalTableFlags.HasFlag(e.EventType))
                .Select(e => new NormalTableLeadAppEventDTO
                {
                    Seen = e.ReadByBroker,
                    AppEventID = e.Id,
                    EventType = EnumExtensions.ConvertEnumFlagsToString(e.EventType),
                    EventTimeStamp = e.EventTimeStamp
                });
            DashboardPerLeadDTO.EmailEvents = EmailEvents
                .Select(e => new NormalTableLeadEmailEventDTO
                {
                    EmailId = e.Id,
                    Seen = e.Seen,
                    NeedsAction = e.NeedsAction,
                    Received = e.TimeReceived,
                    RepliedTo = e.RepliedTo
                });

            DateTime first = DateTime.MinValue;
            var firstEvent = DashboardPerLeadDTO.AppEvents?.FirstOrDefault();
            if (firstEvent != null && !firstEvent.Seen) first = firstEvent.EventTimeStamp;
            //var first = dtoToAdd.AppEvents?.FirstOrDefault()?.EventTimeStamp ?? DateTime.MinValue;
            DateTime second = DateTime.MinValue;
            var secondEvent = DashboardPerLeadDTO.EmailEvents?.FirstOrDefault();
            if (secondEvent != null && !secondEvent.Seen) second = secondEvent.Received;
            //var second = dtoToAdd.EmailEvents?.FirstOrDefault()?.Received ?? DateTime.MinValue;
            DashboardPerLeadDTO.MostRecentEventOrEmailTime = (first) > (second) ?
                (first) : (second);

            //var first = DashboardPerLeadDTO.AppEvents?.FirstOrDefault()?.EventTimeStamp ?? DateTime.MinValue;
            //var second = DashboardPerLeadDTO.EmailEvents?.FirstOrDefault()?.Received ?? DateTime.MinValue;
            //DashboardPerLeadDTO.MostRecentEventOrEmailTime = (first) > (second) ?
            //    (first) : (second);
        }

        if (PriorityTable)
        {
            var NotifsTask = NotifContext.Notifs
            .Where(n => n.BrokerId == brokerId && !n.isSeen && n.LeadId != null)
            .Select(n => new { n.Id, n.LeadId, n.CreatedTimeStamp, n.NotifType, n.priority, n.EventId })
            .OrderBy(n => n.priority)
            .ThenByDescending(n => n.CreatedTimeStamp)
            .ToListAsync();

            var Notifs = await NotifsTask;
            DashboardPerLeadDTO.PriorityNotifs = Notifs.Select(n => new PriorityTableLeadNotifDTO
            {
                NotifID = n.Id,
                Priority = n.priority,
                EventType = n.NotifType.ToString(),
                EmailID = n.EventId,
                EventTimeStamp = n.CreatedTimeStamp
            });
            DashboardPerLeadDTO.HighestPriority = DashboardPerLeadDTO.PriorityNotifs.FirstOrDefault()?.Priority;
        }
        return DashboardPerLeadDTO;
    }

    public async Task<CompleteDashboardDTO> GetAllDashboardNotifs(Guid brokerId)
    {
        //TODO cache info required from broker

        using var AppEventsContext = _contextFactory.CreateDbContext();
        using var EmailEventsContext = _contextFactory.CreateDbContext();
        using var NotifContext = _contextFactory.CreateDbContext();

        var broker = await AppEventsContext.Brokers
            .Select(b => new { b.Id, b.LastSeenAppEventId, b.isAdmin, b.isSolo,b.TimeZoneId })
            .FirstAsync(b => b.Id == brokerId);

        var NormalTableFlags = EventType.LeadAssignedToYou | EventType.LeadStatusChange | EventType.ActionPlanFinished | EventType.ActionPlanStarted;
        if (broker.isAdmin && !broker.isSolo)
        {
            NormalTableFlags |= EventType.LeadCreated | EventType.YouAssignedtoBroker;
        }

        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(broker.TimeZoneId);
        var todaydate = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, timeZoneInfo).Date;
        var todayStart = todaydate.AddMinutes(0);
        var UTCstartDay = MyTimeZoneConverter.ConvertToUTC(timeZoneInfo, todayStart);

        //get All events including non lead-related events that broker should be notified of
        var AppEventsTask = AppEventsContext.AppEvents
            //.Where(e => e.BrokerId == brokerId && e.NotifyBroker && (!e.ReadByBroker || e.EventTimeStamp >= UTCstartDay) && e.Id >= broker.LastSeenAppEventId)
            .Where(e => e.BrokerId == brokerId && e.NotifyBroker && (!e.ReadByBroker || e.EventTimeStamp >= UTCstartDay))
            .Select(e => new { e.Id, e.LeadId, e.EventTimeStamp, e.EventType,e.ReadByBroker, e.Props })
            .OrderBy(e => e.ReadByBroker)
            .ThenByDescending(e => e.EventTimeStamp)
            .ToListAsync();

        var EmailEventsTask = EmailEventsContext.EmailEvents
            //.Where(e => e.BrokerId == brokerId && (!e.Seen || (e.NeedsAction && !e.RepliedTo)) && e.LeadId != null)
            .Where(e => e.BrokerId == brokerId && e.LeadId != null)
            .Select(e => new { e.Id, e.LeadId, e.BrokerEmail, e.Seen, e.NeedsAction, e.RepliedTo, e.TimeReceived })
            .OrderBy(e => e.Seen)
            .ThenByDescending(e => e.TimeReceived)
            .GroupBy(e => e.LeadId)
            .ToListAsync();

        var NotifsTask = NotifContext.Notifs
            .Where(n => n.BrokerId == brokerId && !n.isSeen && n.LeadId != null)
            .Select(n => new { n.Id, n.LeadId, n.CreatedTimeStamp, n.NotifType, n.priority, n.EventId })
            .OrderBy(n => n.priority)
            .ThenByDescending(n => n.CreatedTimeStamp)
            .GroupBy(n => n.LeadId)
            .ToListAsync();

        var AppEvents = await AppEventsTask;
        var EmailEvents = await EmailEventsTask;
        var Notifs = await NotifsTask;

        var AppEventsWithLead = AppEvents.Where(e => e.LeadId != null);
        var AppEventswithoutLead = AppEvents.Where(e => e.LeadId == null);
        var AppEventsGroupedByLead = AppEventsWithLead.GroupBy(n => n.LeadId);

        var AllLeadIDs = AppEventsGroupedByLead.Select(g => (int)g.Key).Union(EmailEvents.Select(e => (int)e.Key)).Union(Notifs.Select(n => (int)n.Key));
        var leads = await AppEventsContext.Leads
            .Where(l => AllLeadIDs.Contains(l.Id))
            .Select(l => new LeadForNotifsDTO
            {
                brokerId = l.BrokerId,
                LeadId = l.Id,
                LeadfirstName = l.LeadFirstName,
                LeadLastName = l.LeadLastName,
                LeadPhone = l.PhoneNumber,
                LeadEmail = l.LeadEmails.FirstOrDefault(e => e.IsMain).EmailAddress,
                LeadStatus = l.LeadStatus.ToString(),
                LastTimeYouViewedLead = l.LastNotifsViewedAt
            })
            .ToListAsync();

        var CompleteDashboardDTO = new CompleteDashboardDTO
        {
            LeadRelatedNotifs = new(AllLeadIDs.Count()),
            OtherNotifs = new(AppEventswithoutLead.Count())
        };
        foreach (var lead in leads)
        {
            var dtoToAdd = new DashboardPerLeadDTO
            {
                LeadId = lead.LeadId,
                LeadUnAssigned = lead.brokerId == null,
                LeadfirstName = lead.LeadfirstName,
                LeadLastName = lead.LeadLastName,
                LeadPhone = lead.LeadPhone,
                LeadEmail = lead.LeadEmail,
                LeadStatus = lead.LeadStatus,
                LastTimeYouViewedLead = lead.brokerId == null ? null : lead.LastTimeYouViewedLead,
                AppEvents = AppEventsGroupedByLead.FirstOrDefault(g => g.Key == lead.LeadId)?
                .Where(e => NormalTableFlags.HasFlag(e.EventType))
                .Select(e => new NormalTableLeadAppEventDTO
                {
                    Seen = e.ReadByBroker,
                    AppEventID = e.Id,
                    EventType = EnumExtensions.ConvertEnumFlagsToString(e.EventType),
                    EventTimeStamp = e.EventTimeStamp
                }),
                EmailEvents = EmailEvents.FirstOrDefault(g => g.Key == lead.LeadId)?.Select(e => new NormalTableLeadEmailEventDTO
                {
                    EmailId = e.Id,
                    Seen = e.Seen,
                    NeedsAction = e.NeedsAction,
                    Received = e.TimeReceived,
                    RepliedTo = e.RepliedTo
                }),
                PriorityNotifs = Notifs.FirstOrDefault(g => g.Key == lead.LeadId)?.Select(n => new PriorityTableLeadNotifDTO
                {
                    NotifID = n.Id,
                    Priority = n.priority,
                    EventType = n.NotifType.ToString(),
                    EmailID = n.EventId,
                    EventTimeStamp = n.CreatedTimeStamp
                })
            };

            if (dtoToAdd.AppEvents != null || dtoToAdd.EmailEvents != null)
            {
                DateTime first = DateTime.MinValue;
                var firstEvent = dtoToAdd.AppEvents?.FirstOrDefault();
                if (firstEvent != null && !firstEvent.Seen) first = firstEvent.EventTimeStamp;
                //var first = dtoToAdd.AppEvents?.FirstOrDefault()?.EventTimeStamp ?? DateTime.MinValue;
                DateTime second = DateTime.MinValue;
                var secondEvent = dtoToAdd.EmailEvents?.FirstOrDefault();
                if(secondEvent != null && !secondEvent.Seen) second = secondEvent.Received;
                //var second = dtoToAdd.EmailEvents?.FirstOrDefault()?.Received ?? DateTime.MinValue;
                dtoToAdd.MostRecentEventOrEmailTime = (first) > (second) ?
                    (first) : (second);
            }
            if (dtoToAdd.PriorityNotifs != null)
                dtoToAdd.HighestPriority = dtoToAdd.PriorityNotifs.FirstOrDefault()?.Priority;

            CompleteDashboardDTO.LeadRelatedNotifs.Add(dtoToAdd);
        }
        CompleteDashboardDTO.OtherNotifs.AddRange(AppEventswithoutLead.Select(e => new AppEventsNonLeadDTO
        {
            AppEventID = e.Id,
            EventType = e.EventType.ToString(),
            EventTimeStamp = e.EventTimeStamp,
            Kes = e.Props
        }));

        CompleteDashboardDTO.LeadRelatedNotifs = CompleteDashboardDTO.LeadRelatedNotifs.OrderByDescending(l => l.MostRecentEventOrEmailTime).ToList();

        return CompleteDashboardDTO;
    }

    public async Task<CompleteDashboardDTO> UpdateNormalTable(Guid brokerId)
    {
        using var AppEventsContext = _contextFactory.CreateDbContext();
        using var EmailEventsContext = _contextFactory.CreateDbContext();

        var broker = await AppEventsContext.Brokers
            .Select(b => new { b.Id, b.LastSeenAppEventId, b.isAdmin, b.isSolo, b.TimeZoneId })
            .FirstAsync(b => b.Id == brokerId);

        var NormalTableFlags = EventType.LeadAssignedToYou | EventType.LeadStatusChange | EventType.ActionPlanFinished | EventType.ActionPlanStarted;
        if (broker.isAdmin && !broker.isSolo)
        {
            NormalTableFlags |= EventType.LeadCreated | EventType.YouAssignedtoBroker;
        }

        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(broker.TimeZoneId);
        var todaydate = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, timeZoneInfo).Date;
        var todayStart = todaydate.AddMinutes(0);
        var UTCstartDay = MyTimeZoneConverter.ConvertToUTC(timeZoneInfo, todayStart);

        //get All events including non lead-related events that broker should be notified of
        var AppEventsTask = AppEventsContext.AppEvents
            //.Where(e => e.BrokerId == brokerId && e.NotifyBroker && (!e.ReadByBroker || e.EventTimeStamp >= UTCstartDay) && e.Id >= broker.LastSeenAppEventId)
            .Where(e => e.BrokerId == brokerId && e.NotifyBroker && (!e.ReadByBroker || e.EventTimeStamp >= UTCstartDay))
            .Select(e => new { e.Id, e.LeadId, e.EventTimeStamp, e.EventType, e.ReadByBroker,e.Props })
            .OrderBy(e => e.ReadByBroker)
            .ThenByDescending(e => e.EventTimeStamp)
            .ToListAsync();

        var EmailEventsTask = EmailEventsContext.EmailEvents
            //.Where(e => e.BrokerId == brokerId && (!e.Seen || (e.NeedsAction && !e.RepliedTo)) && e.LeadId != null)
            .Where(e => e.BrokerId == brokerId && e.LeadId != null)
            .Select(e => new { e.Id, e.LeadId, e.BrokerEmail, e.Seen, e.NeedsAction, e.RepliedTo, e.TimeReceived })
            .OrderBy(e => e.Seen)
            .ThenByDescending(e => e.TimeReceived)
            .GroupBy(e => e.LeadId)
            .ToListAsync();

        var AppEvents = await AppEventsTask;
        var EmailEvents = await EmailEventsTask;

        var AppEventsWithLead = AppEvents.Where(e => e.LeadId != null);
        var AppEventswithoutLead = AppEvents.Where(e => e.LeadId == null);
        var AppEventsGroupedByLead = AppEventsWithLead.GroupBy(n => n.LeadId);

        var AllLeadIDs = AppEventsGroupedByLead.Select(g => (int)g.Key).Union(EmailEvents.Select(e => (int)e.Key));
        var leads = await AppEventsContext.Leads
            .Where(l => AllLeadIDs.Contains(l.Id))
            .Select(l => new LeadForNotifsDTO
            {
                brokerId = l.BrokerId,
                LeadId = l.Id,
                LeadfirstName = l.LeadFirstName,
                LeadLastName = l.LeadLastName,
                LeadPhone = l.PhoneNumber,
                LeadEmail = l.LeadEmails.FirstOrDefault(e => e.IsMain).EmailAddress,
                LeadStatus = l.LeadStatus.ToString(),
                LastTimeYouViewedLead = l.LastNotifsViewedAt
            })
            .ToListAsync();

        var CompleteDashboardDTO = new CompleteDashboardDTO
        {
            LeadRelatedNotifs = new(AllLeadIDs.Count()),
            OtherNotifs = new(AppEventswithoutLead.Count())
        };
        foreach (var lead in leads)
        {
            var dtoToAdd = new DashboardPerLeadDTO
            {
                LeadId = lead.LeadId,
                LeadUnAssigned = lead.brokerId == null,
                LeadfirstName = lead.LeadfirstName,
                LeadLastName = lead.LeadLastName,
                LeadPhone = lead.LeadPhone,
                LeadEmail = lead.LeadEmail,
                LeadStatus = lead.LeadStatus,
                LastTimeYouViewedLead = lead.brokerId == null ? null : lead.LastTimeYouViewedLead,
                AppEvents = AppEventsGroupedByLead.FirstOrDefault(g => g.Key == lead.LeadId)?
                .Where(e => NormalTableFlags.HasFlag(e.EventType))
                .Select(e => new NormalTableLeadAppEventDTO
                {
                    Seen = e.ReadByBroker,
                    AppEventID = e.Id,
                    EventType = EnumExtensions.ConvertEnumFlagsToString(e.EventType),
                    EventTimeStamp = e.EventTimeStamp
                }),
                EmailEvents = EmailEvents.FirstOrDefault(g => g.Key == lead.LeadId)?.Select(e => new NormalTableLeadEmailEventDTO
                {
                    EmailId = e.Id,
                    Seen = e.Seen,
                    NeedsAction = e.NeedsAction,
                    Received = e.TimeReceived,
                    RepliedTo = e.RepliedTo
                })
            };
            if (dtoToAdd.AppEvents != null || dtoToAdd.EmailEvents != null)
            {
                DateTime first = DateTime.MinValue;
                var firstEvent = dtoToAdd.AppEvents?.FirstOrDefault();
                if (firstEvent != null && !firstEvent.Seen) first = firstEvent.EventTimeStamp;
                //var first = dtoToAdd.AppEvents?.FirstOrDefault()?.EventTimeStamp ?? DateTime.MinValue;
                DateTime second = DateTime.MinValue;
                var secondEvent = dtoToAdd.EmailEvents?.FirstOrDefault();
                if (secondEvent != null && !secondEvent.Seen) second = secondEvent.Received;
                //var second = dtoToAdd.EmailEvents?.FirstOrDefault()?.Received ?? DateTime.MinValue;
                dtoToAdd.MostRecentEventOrEmailTime = (first) > (second) ?
                    (first) : (second);
            }
            CompleteDashboardDTO.LeadRelatedNotifs.Add(dtoToAdd);
        }
        CompleteDashboardDTO.OtherNotifs.AddRange(AppEventswithoutLead.Select(e => new AppEventsNonLeadDTO
        {
            AppEventID = e.Id,
            EventType = e.EventType.ToString(),
            EventTimeStamp = e.EventTimeStamp,
            Kes = e.Props
        }));

        CompleteDashboardDTO.LeadRelatedNotifs = CompleteDashboardDTO.LeadRelatedNotifs.OrderByDescending(l => l.MostRecentEventOrEmailTime).ToList();

        return CompleteDashboardDTO;
    }

    public async Task<CompleteDashboardDTO> UpdatePriorityTable(Guid brokerId)
    {
        using var NotifContext = _contextFactory.CreateDbContext();

        var Notifs = await NotifContext.Notifs
            .Where(n => n.BrokerId == brokerId && !n.isSeen && n.LeadId != null)
            .Select(n => new { n.Id, n.LeadId, n.CreatedTimeStamp, n.NotifType, n.priority, n.EventId })
            .OrderBy(n => n.priority)
            .ThenByDescending(n => n.CreatedTimeStamp)
            .GroupBy(n => n.LeadId)
        .ToListAsync();

        var AllLeadIDs = Notifs.Select(n => (int)n.Key);
        var leads = await NotifContext.Leads
            .Where(l => AllLeadIDs.Contains(l.Id))
            .Select(l => new LeadForNotifsDTO
            {
                brokerId = l.BrokerId,
                LeadId = l.Id,
                LeadfirstName = l.LeadFirstName,
                LeadLastName = l.LeadLastName,
                LeadPhone = l.PhoneNumber,
                LeadEmail = l.LeadEmails.FirstOrDefault(e => e.IsMain).EmailAddress,
                LeadStatus = l.LeadStatus.ToString(),
                LastTimeYouViewedLead = l.LastNotifsViewedAt
            })
            .ToListAsync();
        var broker = await NotifContext.Brokers
           .Select(b => new { b.Id, b.LastSeenAppEventId, b.isAdmin, b.isSolo })
           .FirstAsync(b => b.Id == brokerId);

        var CompleteDashboardDTO = new CompleteDashboardDTO
        {
            LeadRelatedNotifs = new(AllLeadIDs.Count()),
        };
        foreach (var lead in leads)
        {
            var dtoToAdd = new DashboardPerLeadDTO
            {
                LeadId = lead.LeadId,
                LeadUnAssigned = lead.brokerId == null,
                LeadfirstName = lead.LeadfirstName,
                LeadLastName = lead.LeadLastName,
                LeadPhone = lead.LeadPhone,
                LeadEmail = lead.LeadEmail,
                LeadStatus = lead.LeadStatus,
                LastTimeYouViewedLead = broker.isAdmin ? null : lead.LastTimeYouViewedLead,
                PriorityNotifs = Notifs.FirstOrDefault(g => g.Key == lead.LeadId)?.Select(n => new PriorityTableLeadNotifDTO
                {
                    NotifID = n.Id,
                    Priority = n.priority,
                    EventType = n.NotifType.ToString(),
                    EmailID = n.EventId,
                    EventTimeStamp = n.CreatedTimeStamp
                })
            };
            dtoToAdd.HighestPriority = dtoToAdd.PriorityNotifs?.FirstOrDefault()?.Priority;

            CompleteDashboardDTO.LeadRelatedNotifs.Add(dtoToAdd);
        }
        return CompleteDashboardDTO;
    }
}
