using Core.Domain.NotificationAggregate;
using Microsoft.AspNetCore.SignalR;

namespace Web.RealTimeNotifs;

public class RealTimeNotifSender
{
    private readonly IHubContext<NotifsHub> _hubContext;
    public RealTimeNotifSender(IHubContext<NotifsHub> hubContext)
    {
        _hubContext = hubContext;
    }
    /// <summary>
    /// 2 for listing Assigned
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="brokerId"></param>
    /// <param name="appEvent"></param>
    /// <returns></returns>
    public async Task RealTimeNotifOneEvent(ILogger logger, Guid brokerId, AppEvent appEvent)
    {
        if(appEvent.EventType == EventType.ListingAssigned)
        {
            await _hubContext.Clients.User(brokerId.ToString())
                .SendAsync("ReceiveMessage", "2");
        }
    }
    public async Task RealTimeNotifAFewAppEvents(ILogger logger, Guid brokerId, List<AppEvent> appEvents)
    {
        throw new NotImplementedException();
    }
    /// <summary>
    /// contains logic to send to frontend the proper
    /// notif structure (ID, category appEvent/EmailEvent/Notif), which endpoint to call to get updates
    /// which endpoint to call to mark seen, etc
    /// 0 priority, 1 other events
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public async Task SendRealTimeNotifsAsync(ILogger logger, Guid brokerId, bool browser, bool PushNotif, List<Notif>? analyzerNotifs, List<AppEvent>? appEvents, List<EmailEvent>? emailEvents)
    {
        //should never fail
        try
        {
            if (analyzerNotifs != null && analyzerNotifs.Any())
            {
                Console.WriteLine("Sending AnalyzerNotifs");
                await _hubContext.Clients.User(brokerId.ToString())
                    .SendAsync("ReceiveMessage", "0");
                return;
            }
            else //appEvents and emailEvents
            {
                await _hubContext.Clients.User(brokerId.ToString())
                    .SendAsync("ReceiveMessage", "1");
                //if (appEvents != null && appEvents.Any())
                //{
                //    var appEventsByBroker = appEvents.GroupBy(e => e.BrokerId);
                //    foreach (var grp in appEventsByBroker)
                //    {
                //        var distinctEventsLeadIDs = grp.Select(e => e.LeadId).Distinct();
                //        if (grp.Key == brokerId) //This broker, might also have emails
                //        {
                //            if (emailEvents != null && emailEvents.Any())
                //            //later might have emailEvents that belong to other brokers
                //            {
                //                distinctEventsLeadIDs = distinctEventsLeadIDs.Union(emailEvents.Select(m => m.LeadId).Distinct());
                //            }
                //        }
                //        //update normalTable, give count of distinctIDs to backend
                //    }
                //}
                //else if (emailEvents != null && emailEvents.Any())
                //{
                //    var emailEventsByBroker = emailEvents.Select(m => m.LeadId).Distinct();
                //    //update normalTable, give count of distinctIDs to backend
                //}
            }
        }
        // FOR NOW IGNORE PUSH NOTIFS    
        catch (Exception ex)
        {
            logger.LogError("{tag} failed sending real time notifs with error {error}", "realtimenotifs", ex.Message);
        }
    }
}
