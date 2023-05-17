using Core.Domain.NotificationAggregate;
using Infrastructure.Data;
using MediatR;

namespace Web.Outbox.Config;

public abstract class EventHandlerBase<TEvent> : INotificationHandler<TEvent> where TEvent : EventBase
{
    public readonly AppDbContext _context;
    public readonly ILogger _logger;
    public EventHandlerBase(AppDbContext appDbContext, ILogger logger)
    {
        _context = appDbContext;
        _logger = logger;
    }
    public abstract Task Handle(TEvent appEvent, CancellationToken cancellationToken);

    /// <summary>
    /// If AppEvent.DeleteAfterProcessin == true deletes event 
    /// Else marks its ProcessingStatus as Done
    /// then Removes event from Scheduled Dictionary and Error Dictionary juste au cas ou.
    /// Saves database
    /// </summary>
    /// <param name="notif"></param>
    /// <returns></returns>
    public async Task FinishProcessing(AppEvent AppEvent)
    {
        if (AppEvent.DeleteAfterProcessing)
        {
            _context.AppEvents.Remove(AppEvent);

        }
        else
        {
            AppEvent.ProcessingStatus = ProcessingStatus.Done;
        }
        await _context.SaveChangesAsync();
        OutboxMemCache.SchedulingErrorDict.TryRemove(AppEvent.Id, out var removed);
    }
}
