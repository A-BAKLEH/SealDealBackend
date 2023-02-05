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
  public abstract Task Handle(TEvent notification, CancellationToken cancellationToken);

  /// <summary>
  /// If Notif.DeleteAfterProcessin == true deletes Notif 
  /// Else marks its ProcessingStatus as Done
  /// then Removes Notif from Scheduled Dictionary and Error Dictionary juste au cas ou.
  /// Saves database
  /// </summary>
  /// <param name="notif"></param>
  /// <returns></returns>
  public async Task FinishProcessing(Notification notif)
  {
    if(notif.DeleteAfterProcessing)
    {
      _context.Notifications.Remove(notif);
      
    }
    else
    {
      notif.ProcessingStatus = ProcessingStatus.Done;
    }
    OutboxMemCache.ScheduledDict.Remove(notif.Id);
    OutboxMemCache.SchedulingErrorDict.Remove(notif.Id);

    await _context.SaveChangesAsync();
  }
}
