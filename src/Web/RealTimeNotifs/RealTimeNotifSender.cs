using Core.Domain.NotificationAggregate;

namespace Web.RealTimeNotifs;

public static class RealTimeNotifSender
{

    public static async Task RealTimeNotifOneEvent(ILogger logger, Guid brokerId, AppEvent appEvent)
    {
        if(appEvent.EventType == EventType.ListingAssigned)
        {
            //send listing assigned, id and type of event
        }
    }
    public static async Task RealTimeNotifAFewAppEvents(ILogger logger, Guid brokerId, List<AppEvent> appEvents)
    {

    }
    /// <summary>
    /// contains logic to send to frontend the proper
    /// notif structure (ID, category appEvent/EmailEvent/Notif), which endpoint to call to get updates
    /// which endpoint to call to mark seen, etc
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static async Task SendRealTimeNotifsAsync(ILogger logger, Guid brokerId, bool browser, bool PushNotif, List<Notif>? analyzerNotifs, List<AppEvent>? appEvents, List<EmailEvent>? emailEvents)
    {
        //should never fail
        try
        {
            if (analyzerNotifs != null && analyzerNotifs.Any())
            {
                Console.WriteLine("Sending AnalyzerNotifs");
                //UpdatePriority table
                return;
            }
            else //appEvents and emailEvents
            {
                if (appEvents != null && appEvents.Any())
                {
                    var appEventsByBroker = appEvents.GroupBy(e => e.BrokerId);
                    foreach (var grp in appEventsByBroker)
                    {
                        var distinctEventsLeadIDs = grp.Select(e => e.LeadId).Distinct();
                        if (grp.Key == brokerId) //This broker, might also have emails
                        {
                            if (emailEvents != null && emailEvents.Any())
                                //later might have emailEvents that belong to other brokers
                            {
                                distinctEventsLeadIDs = distinctEventsLeadIDs.Union(emailEvents.Select(m => m.LeadId).Distinct());
                            }
                        }                     
                        //update normalTable, give count of distinctIDs to backend
                    }
                }
                else if (emailEvents != null && emailEvents.Any())
                {
                    var emailEventsByBroker = emailEvents.Select(m => m.LeadId).Distinct();
                    //update normalTable, give count of distinctIDs to backend
                }
            }
            Console.WriteLine("Sending RealTimeNotifs");
            await Task.Delay(500);
        }
        // FOR NOW IGNORE PUSH NOTIFS    
        catch (Exception ex)
        {
            logger.LogError("{place} failed sending real time notifs with error {error}", "realtimenotifs", ex.Message);
        }
    }
}
