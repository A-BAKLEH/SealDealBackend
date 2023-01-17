using Core.Domain.NotificationAggregate;
using Infrastructure.Data;
using Web.Outbox.Config;

namespace Web.Outbox;

public class ListingUnAssigned : EventBase
{
}

/// <summary>
/// SignalR to Broker's Browser and Push Notif to Phone
/// </summary>
public class ListingUnAssignedHandler : EventHandlerBase<ListingUnAssigned>
{
  public ListingUnAssignedHandler(AppDbContext appDbContext, ILogger<ListingUnAssignedHandler> logger) : base(appDbContext, logger)
  {
  }

  public override async Task Handle(ListingUnAssigned ListingUnAssigned, CancellationToken cancellationToken)
  {
    Notification? notif = null;
    try
    {
      //process
      notif = _context.Notifications.FirstOrDefault(x => x.Id == ListingUnAssigned.NotifId);
      if (notif == null) { _logger.LogError("No Notif with NotifId {NotifId}", ListingUnAssigned.NotifId); return; }

      Console.WriteLine("from handler:" + ListingUnAssigned.NotifId);
      //TODO SignalR and PushNotif

      await this.FinishProcessing(notif);
    }
    catch (Exception ex)
    {
      _logger.LogError("Handling ListingUnAssigned Failed for notif with notifId {notifId} with error {error}", ListingUnAssigned.NotifId, ex.Message);
      notif.ProcessingStatus = ProcessingStatus.Failed;
      await _context.SaveChangesAsync();
    }
  }
}
