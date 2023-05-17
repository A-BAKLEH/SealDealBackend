using System.Collections.Concurrent;

namespace Web.Outbox.Config;

public static class OutboxMemCache
{
    /// <summary>
    /// for events that could not be scheduled
    /// appEventId, SceduledEvent or Event supposed to be Scheduled
    /// When scheduled successfully IT HAS TO BE DELETED
    /// </summary>
    public static ConcurrentDictionary<int, EventBase> SchedulingErrorDict =
        new ConcurrentDictionary<int, EventBase>();
}
