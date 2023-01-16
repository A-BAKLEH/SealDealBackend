namespace Web.Outbox.Config;

public static class OutboxMemCache
{
  /// <summary>
  /// notifId, SceduledEvent or Event supposed to be Scheduled
  /// Add EventBase to It when there is an error with the scheduling Of the HangfireJob that will process the
  /// notif with notifId
  /// When Handled IT HAS TO BE DELETED
  /// </summary>
  public static Dictionary<int, EventBase> ErrorDictionary = new Dictionary<int, EventBase>();

  /// <summary>
  /// notifId, HangfireJobId that will publish the Event
  /// For now add scheduled JobID to this dictionary, delete it once processed inside the EventBase
  /// u can probably later add a boolean that acts as a lock for processing the notif in case multiple 
  /// server threads try to process the same job
  /// </summary>
  public static Dictionary<int, string> ScheduledDictionary = new Dictionary<int, string>();
}
