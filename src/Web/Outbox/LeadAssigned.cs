using Core.Domain.NotificationAggregate;
using Infrastructure.Data;
using Web.Outbox.Config;
using Web.RealTimeNotifs;

namespace Web.Outbox;

/// <summary>
/// when admin assign lead to broker. Handle ActionPlan or whatever if needed
/// especially when lead is created in-request. Also run signalR to notify broker of new lead if needed
/// </summary>
public class LeadAssigned : EventBase
{
}
public class LeadAssignedHandler : EventHandlerBase<LeadAssigned>
{
    public LeadAssignedHandler(AppDbContext appDbContext, ILogger<LeadAssignedHandler> logger) : base(appDbContext, logger)
    {
    }

    public override async Task Handle(LeadAssigned LeadAssignedEvent, CancellationToken cancellationToken)
    {
        AppEvent? appEvent = null;
        try
        {
            //process
            appEvent = _context.AppEvents.FirstOrDefault(x => x.Id == LeadAssignedEvent.AppEventId);
            if (appEvent == null) { _logger.LogError("No appEvent with Id {AppEventId}", LeadAssignedEvent.AppEventId); return; }

            if (appEvent.ProcessingStatus != ProcessingStatus.Done)
            {
                //TODO notify broker now if he's online and send PushNotif
                await RealTimeNotifSender.SendRealTimeNotifsAsync(appEvent.BrokerId,true, true, new List<AppEvent>(1) { appEvent }, null);
            }
            await this.FinishProcessing(appEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError("Handling ListingAssigned Failed for appEvent with appEventId {AppEventId} with error {error}", LeadAssignedEvent.AppEventId, ex.Message);
            appEvent.ProcessingStatus = ProcessingStatus.Failed;
            await _context.SaveChangesAsync();
            throw;
        }
    }
}
