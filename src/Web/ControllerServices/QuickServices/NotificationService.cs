using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using EventType = Core.Domain.NotificationAggregate.EventType;

namespace Web.ControllerServices.QuickServices;
public class NotificationService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    public NotificationService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<dynamic> GetAllDashboardNotifs(Guid brokerId)
    {
        using var AppEventsContext = _contextFactory.CreateDbContext();
        using var EmailEventsContext = _contextFactory.CreateDbContext();
        using var NotifContext = _contextFactory.CreateDbContext();

        var broker = await AppEventsContext.Brokers
            .Select(b => new { b.Id, b.LastSeenAppEventId, b.isAdmin, b.isSolo })
            .FirstAsync(b => b.Id == brokerId);
        
        var NormalTableFlags = (int)(EventType.LeadAssigned | EventType.LeadStatusChange | EventType.ActionPlanFinished | EventType.ActionPlanEmailSent);
        if(broker.isAdmin && !broker.isSolo)
        {
            NormalTableFlags |= (int)(EventType.LeadCreated);
        }
        
        var AppEventsTask = AppEventsContext.AppEvents
            .Where(e => e.BrokerId == brokerId && e.NotifyBroker && !e.ReadByBroker && e.Id >= broker.LastSeenAppEventId)
            .Select(e => new { e.Id, e.LeadId, e.EventTimeStamp, e.EventType, e.Props })
            .ToListAsync();

        var EmailEventsTask = EmailEventsContext.EmailEvents
            .Where(e => e.BrokerId == brokerId && !e.Seen && e.LeadId != null)
            .Select(e => new { e.Id, e.LeadId, e.BrokerEmail, e.Seen, e.TimeReceived })
            .GroupBy(e => e.LeadId)
            .ToListAsync();

        var NotifsTask = NotifContext.Notifs
            .Where(n => n.BrokerId == brokerId && !n.isSeen)
            .Select(n => new { n.Id, n.LeadId, n.CreatedTimeStamp, n.NotifType, n.priority })
            .ToListAsync();

        var AppEvents = await AppEventsTask;
        var EmailEvents = await EmailEventsTask;
        var Notifs = await NotifsTask;

        var AppEventsWithLead = AppEvents.Where(e => e.LeadId != null).ToList();
        var AppEventswithoutLead = AppEvents.Where(e => e.LeadId == null).ToList();

        //var NormalTableEvents

        return null;

    }
}
