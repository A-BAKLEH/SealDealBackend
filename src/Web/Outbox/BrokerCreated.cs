using Core.Domain.NotificationAggregate;
using Infrastructure.Data;
using Web.Outbox.Config;

namespace Web.Outbox;

/// <summary>
/// Sends Email with temp password to Broker.
/// </summary>
public class BrokerCreated : EventBase
{
}
public class BrokerCreatedHandler : EventHandlerBase<BrokerCreated>
{
    public BrokerCreatedHandler(AppDbContext appDbContext, ILogger<BrokerCreatedHandler> logger) : base(appDbContext, logger)
    {
    }

    public override async Task Handle(BrokerCreated BrokerCreatedEvent, CancellationToken cancellationToken)
    {
        AppEvent? appEvent = null;
        try
        {
            //process
            appEvent = _context.AppEvents.FirstOrDefault(x => x.Id == BrokerCreatedEvent.AppEventId);
            if (appEvent == null) { _logger.LogError("No appEvent with NotifId {NotifId}", BrokerCreatedEvent.AppEventId); return; }

            if (appEvent.ProcessingStatus != ProcessingStatus.Done)
            {
                //TODO SEND EMAIL
                //NotificationJSONKeys.TempPasswd contains password. DELETE IT AFTER SENDING EMAIL
            }
            await this.FinishProcessing(appEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError("Handling BrokerCreated Failed for appEvent with appEventId {AppEventId} with error {error}", BrokerCreatedEvent.AppEventId, ex.Message);
            appEvent.ProcessingStatus = ProcessingStatus.Failed;
            await _context.SaveChangesAsync();
            throw;
        }
    }
}
