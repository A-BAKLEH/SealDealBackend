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
    public static async Task SendRealTimeNotifsAsync(Guid brokerId,bool browser, bool PushNotif, List<AppEvent>? events, List<Notif>? notifs )
    {
        Console.WriteLine("Sending RealTimeNotifs");
        await Task.Delay(1000);
    }
}
