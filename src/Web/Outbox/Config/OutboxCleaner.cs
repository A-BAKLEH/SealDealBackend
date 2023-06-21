using Core.Config.Constants.LoggingConstants;

namespace Web.Outbox.Config;

public class OutboxCleaner
{
    private readonly ILogger<OutboxCleaner> _logger;
    public OutboxCleaner(ILogger<OutboxCleaner> logger)
    {
        _logger = logger;
    }
    public void CleanOutbox()
    {
        var remove = new List<int>();
        foreach (var pair in OutboxMemCache.SchedulingErrorDict)
        {
            try
            {
                Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(pair.Value));             
            }
            catch(Exception ex)
            {
                if(pair.Value is BrokerCreated)
                {
                    _logger.LogCritical("{tag} brokerCreated event with eventId {eventId} failed scheduling hangfire." +
                        " Send password manually. error : {error}", TagConstants.OutboxCleaner,pair.Key,ex.Message);
                }
            }
            finally
            {
                remove.Add(pair.Key);
            }          
        }
        foreach (var key in remove)
        {
            OutboxMemCache.SchedulingErrorDict.Remove(key, out var asd);
        }
        _logger.LogWarning("{tag} there were {failedOutboxCount} failed events in hangfire outbox", TagConstants.OutboxCleaner, remove.Count);
    }
}
