using Core.Domain.NotificationAggregate;

namespace Web.RealTimeNotifs;

public static class RealTimeNotifSender
{
    /// <summary>
    /// contains logic to send to frontend the proper
    /// notif structure (ID, category appEvent/EmailEvent/Notif), which endpoint to call to get updates
    /// which endpoint to call to mark seen, etc
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static async Task SendRealTimeNotifsAsync(ILogger logger,Guid brokerId, bool browser, bool PushNotif, List<AppEvent>? appEvents, List<EmailEvent>? emailEvents)
    {
        //should never fail
        try
        {
            Console.WriteLine("Sending RealTimeNotifs");
            await Task.Delay(1000);
        }
        // FOR NOW IGNORE PUSH NOTIFS    
        catch (Exception ex)
        {
            logger.LogError("{place} failed sending real time notifs with error {error}", "realtimenotifs",ex.Message);
        }
    }
}
