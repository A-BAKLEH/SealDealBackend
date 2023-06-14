using Core.Config.Constants.LoggingConstants;
using Core.Domain.NotificationAggregate;
using Infrastructure.Data;
using Web.Outbox.Config;
using Web.RealTimeNotifs;

namespace Web.Outbox;

/// <summary>
/// SignalR to Broker's Browser and Push Notif to Phone
/// </summary>
public class ListingAssigned : EventBase
{
}


public class ListingAssignedHandler : EventHandlerBase<ListingAssigned>
{
    public ListingAssignedHandler(AppDbContext appDbContext, ILogger<ListingAssignedHandler> logger) : base(appDbContext, logger)
    {
    }

    public override async Task Handle(ListingAssigned listingAssignedEvent, CancellationToken cancellationToken)
    {
        AppEvent? appEvent = null;
        try
        {
            //process
            appEvent = _context.AppEvents.FirstOrDefault(x => x.Id == listingAssignedEvent.AppEventId);
            if (appEvent == null) { _logger.LogError("No appEvent with Id {AppEventId}", listingAssignedEvent.AppEventId); return; }

            if (appEvent.ProcessingStatus != ProcessingStatus.Done)
            {
                //TODO notify broker now if he's online and send PushNotif
                await RealTimeNotifSender.RealTimeNotifOneEvent(_logger, appEvent.BrokerId, appEvent);
            }
            await this.FinishProcessing(appEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError("{tag} Handling ListingAssigned Failed for appEvent with appEventId {appEventId} with error {error}",TagConstants.handleListingAssigned ,listingAssignedEvent.AppEventId, ex.Message);
            appEvent.ProcessingStatus = ProcessingStatus.Failed;
            await _context.SaveChangesAsync();
            throw;
        }
    }
}
