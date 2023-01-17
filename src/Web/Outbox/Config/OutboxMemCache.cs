namespace Web.Outbox.Config;

public static class OutboxMemCache
{
  /// <summary>
  /// for events that could not be scheduled
  /// notifId, SceduledEvent or Event supposed to be Scheduled
  /// When scheduled successfully IT HAS TO BE DELETED
  /// </summary>
  public static Dictionary<int, EventBase> SchedulingErrorDict = new Dictionary<int, EventBase>();

  /// <summary>
  /// notifId, HangfireJobId
  /// For now add scheduled JobID to this dictionary, delete it once processed inside the EventBase
  /// 
  /// u can probably later add a boolean that acts as a lock for processing the notif in case multiple 
  /// server threads try to process the same job
  /// </summary>
  public static Dictionary<int, string> ScheduledDict = new Dictionary<int, string>();
}
