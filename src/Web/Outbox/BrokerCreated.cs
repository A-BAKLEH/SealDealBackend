using Core.Domain.NotificationAggregate;
using Infrastructure.Data;
using Web.Constants;
using Web.Outbox.Config;

namespace Web.Outbox;

public class BrokerCreated : EventBase
{
}

/// <summary>
/// Sends Email with temp password to Broker and sets EmailSent to True in Notif Props.
/// </summary>
public class BrokerCreatedHandler : EventHandlerBase<BrokerCreated>
{
  public BrokerCreatedHandler(AppDbContext appDbContext, ILogger<BrokerCreatedHandler> logger) : base(appDbContext, logger)
  {
  }

  public override async Task Handle(BrokerCreated BrokerCreatedEvent, CancellationToken cancellationToken)
  {
    Notification? notif = null;
    try
    {
      //process
      notif = _context.Notifications.FirstOrDefault(x => x.Id == BrokerCreatedEvent.NotifId);
      if(notif == null) { _logger.LogError("No Notif with NotifId {NotifId}",BrokerCreatedEvent.NotifId); return; }

      if(notif.NotifProps[NotificationJSONKeys.EmailSent] == "0")
      {
        //TODO SEND EMAIL
        var tempPassword = notif.NotifProps[NotificationJSONKeys.TempPasswd];

        notif.NotifProps[NotificationJSONKeys.EmailSent] = "1";
        _context.Notifications.Update(notif);
      }
      Console.WriteLine("from handler:" + BrokerCreatedEvent.NotifId);
      await this.FinishProcessing(notif);
    }
    catch(Exception ex)
    {
      _logger.LogError("Handling BrokerCreated Failed for notif with notifId {notifId} with error {error}", BrokerCreatedEvent.NotifId,ex.Message);
      notif.ProcessingStatus = ProcessingStatus.Failed;
      await _context.SaveChangesAsync();
      throw;
    }
  }
}
