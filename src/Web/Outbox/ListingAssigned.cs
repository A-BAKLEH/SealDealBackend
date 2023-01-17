using Core.Domain.NotificationAggregate;
using Infrastructure.Data;
using Web.Outbox.Config;

namespace Web.Outbox;

public class ListingAssigned : EventBase
{
}

/// <summary>
/// SignalR to Broker's Browser and Push Notif to Phone
/// </summary>
public class ListingAssignedHandler : EventHandlerBase<ListingAssigned>
{
  public ListingAssignedHandler(AppDbContext appDbContext, ILogger<ListingAssignedHandler> logger) : base(appDbContext, logger)
  {
  }

  public override async Task Handle(ListingAssigned listingAssigned, CancellationToken cancellationToken)
  {
    Notification? notif = null;
    try
    {
      //process
      notif = _context.Notifications.FirstOrDefault(x => x.Id == listingAssigned.NotifId);
      if (notif == null) { _logger.LogError("No Notif with NotifId {NotifId}", listingAssigned.NotifId); return; }

      Console.WriteLine("from handler:" + listingAssigned.NotifId);
      //TODO SignalR and PushNotif

      await this.FinishProcessing(notif);
    }
    catch (Exception ex)
    {
      _logger.LogError("Handling ListingAssigned Failed for notif with notifId {notifId} with error {error}", listingAssigned.NotifId, ex.Message);
      notif.ProcessingStatus = ProcessingStatus.Failed;
      await _context.SaveChangesAsync();
    }
  }
}
