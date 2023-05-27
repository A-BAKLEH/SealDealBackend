using Core.DTOs.NotifsDTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Web.Config.EnumExtens;
using EventType = Core.Domain.NotificationAggregate.EventType;

namespace Web.ControllerServices.QuickServices;
public class NotificationService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    public NotificationService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CompleteDashboardDTO> GetAllDashboardNotifs(Guid brokerId)
    {
        using var AppEventsContext = _contextFactory.CreateDbContext();
        using var EmailEventsContext = _contextFactory.CreateDbContext();
        using var NotifContext = _contextFactory.CreateDbContext();

        var broker = await AppEventsContext.Brokers
            .Select(b => new { b.Id, b.LastSeenAppEventId, b.isAdmin, b.isSolo })
            .FirstAsync(b => b.Id == brokerId);

        //var NormalTableFlagsInt = (int)(EventType.LeadAssignedToYou | EventType.LeadStatusChange | EventType.ActionPlanFinished | EventType.ActionPlanEmailSent);
        //if (broker.isAdmin && !broker.isSolo)
        //{
        //    NormalTableFlagsInt |= (int)(EventType.LeadCreated | EventType.YouAssignedtoBroker);
        //}
        var NormalTableFlags = EventType.LeadAssignedToYou | EventType.LeadStatusChange | EventType.ActionPlanFinished | EventType.ActionPlanEmailSent;
        if (broker.isAdmin && !broker.isSolo)
        {
            NormalTableFlags |= EventType.LeadCreated | EventType.YouAssignedtoBroker;
        }

        //get All events including non lead-related events that broker should be notified of
        var AppEventsTask = AppEventsContext.AppEvents
            .Where(e => e.BrokerId == brokerId && e.NotifyBroker && !e.ReadByBroker && e.Id >= broker.LastSeenAppEventId)
            .Select(e => new { e.Id, e.LeadId, e.EventTimeStamp, e.EventType })
            .OrderByDescending(e => e.EventTimeStamp)
            .ToListAsync();

        var EmailEventsTask = EmailEventsContext.EmailEvents
            .Where(e => e.BrokerId == brokerId && (!e.Seen || (e.NeedsAction && !e.RepliedTo)) && e.LeadId != null)
            .Select(e => new { e.Id, e.LeadId, e.BrokerEmail, e.Seen, e.NeedsAction, e.RepliedTo, e.TimeReceived })
            .OrderByDescending(e => e.TimeReceived)
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
            CompleteDashboardDTO.LeadRelatedNotifs.Add(new DashboardPerLeadDTO
            {
                LeadId = lead.LeadId,
                LeadfirstName = lead.LeadfirstName,
                LeadLastName = lead.LeadLastName,
                LeadPhone = lead.LeadPhone,
                LeadEmail = lead.LeadEmail,
                LeadStatus = lead.LeadStatus,
                LastTimeYouViewedLead = lead.LastTimeYouViewedLead,
                AppEvents = AppEventsGroupedByLead.FirstOrDefault(g => g.Key == lead.LeadId)?
                .Where(e => NormalTableFlags.HasFlag(e.EventType))
                .Select(e => new NormalTableLeadAppEventDTO
                {
                    AppEventID = e.Id,
                    EventType = string.Join('&', e.EventType.GetIndividualFlags().Select(f => f.ToString())),
                    EventTimeStamp = e.EventTimeStamp
                }),
                EmailEvents = EmailEvents.FirstOrDefault(g => g.Key == lead.LeadId)?.Select(e => new NormalTableLeadEmailEventDTO
                {
                    EmailId = e.Id,
                    Seen = e.Seen,
                    NeedsAction = e.NeedsAction,
                    Received = e.TimeReceived,
                    RepliedTo = e.RepliedTo
                }).ToList(),
                PriorityNotifs = Notifs.FirstOrDefault(g => g.Key == lead.LeadId)?.Select(n => new PriorityTableLeadNotifDTO
                {
                    NotifID = n.Id,
                    Priority = n.priority,
                    EventType = n.NotifType.ToString(),
                    EmailID = n.EventId,
                    EventTimeStamp = n.CreatedTimeStamp
                }).ToList()
            });
        }
        CompleteDashboardDTO.OtherNotifs.AddRange(AppEventswithoutLead.Select(e => new AppEventsNonLeadDTO
        {
            AppEventID = e.Id,
            EventType = e.EventType.ToString(),
            EventTimeStamp = e.EventTimeStamp
        }));
        return CompleteDashboardDTO;
    }
}
