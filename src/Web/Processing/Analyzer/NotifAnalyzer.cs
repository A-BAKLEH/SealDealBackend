using Core.Domain.NotificationAggregate;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Processing.Analyzer;

public class NotifAnalyzer
{
    private readonly ILogger<NotifAnalyzer> _logger;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    public NotifAnalyzer(IDbContextFactory<AppDbContext> contextFactory, ILogger<NotifAnalyzer> logger)
    {
        _logger = logger;
        _contextFactory = contextFactory;
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

        var broker = await dbcontext.Brokers.FindAsync(brokerId);

        //LastSeenNotifId
        //LastSeenTimeStamp

        //unseen New lead events  > 15 mins 

        var AllUnseenAppEventsWNotifyTrue = await dbcontext.AppEvents
            .Where(x => x.BrokerId == brokerId && x.NotifyBroker == true && x.ReadByBroker == false && x.Id > broker.LastSeenNotifId && x.EventTimeStamp >= broker.LastSeenTimeStamp)
            .OrderBy(x => x.Id)
            .AsNoTracking()
            .ToListAsync();

        //from AllUnseenAppEventsWNotifyTrue get
        //  1) unseen LeadAssigned events > 15 mins  (1)
        //  2) app events unseen for > 1 days EXCEPT LeadAssigned and LeadCreated (4)


        //if admin get all unassigned created Leads that have been unassigned for 1 > hours (priority 1)
        
        var NowMinusOneHour = TimeNow - TimeSpan.FromHours(1);

        var notifs = new List<Notif>();

        if (broker.isAdmin)
        {
            var unassignedCreatedLeads = await dbcontext.Leads
                .Where(x => x.AgencyId == broker.AgencyId && x.BrokerId == null && x.EntryDate <= NowMinusOneHour)
                .OrderBy(x => x.Id)
                .AsNoTracking()
                .ToListAsync();
            unassignedCreatedLeads.ForEach(l => notifs.Add(new Notif
            {
                BrokerId = brokerId,
                LeadId = l.Id,
                CreatedTimeStamp = TimeNow,
                NotifType = EventType.UnAssignedNewLead,
                priority = 1,
            }
            ));
        }
        var sdf = new Notif { };
    }
}
