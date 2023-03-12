using Core.Domain.NotificationAggregate;
using Infrastructure.Data;
using Web.Constants;
using Web.Outbox.Config;

namespace Web.Outbox;

public class StripeSubsChange : EventBase
{
}

/// <summary>
/// Send Email To Admin and mark EmailSent to True in Notif Props
/// </summary>
public class StripeSubsChangeHandler : EventHandlerBase<StripeSubsChange>
{
  public StripeSubsChangeHandler(AppDbContext appDbContext, ILogger<StripeSubsChangeHandler> logger) : base(appDbContext, logger)
  {
  }

  public override async Task Handle(StripeSubsChange stripeSubsChange, CancellationToken cancellationToken)
  {
    Notification? notif = null;
    try
    {
      //process
      notif = _context.Notifications.FirstOrDefault(x => x.Id ==stripeSubsChange.NotifId);
      if (notif == null) { _logger.LogError("No Notif with NotifId {NotifId}", stripeSubsChange.NotifId); return; }

      Console.WriteLine("from handler:" + stripeSubsChange.NotifId);
      if (notif.NotifProps[NotificationJSONKeys.EmailSent] == "0")
      {
        //TODO SEND EMAIL

        notif.NotifProps[NotificationJSONKeys.EmailSent] = "1";
        _context.Notifications.Update(notif);
      }
      await this.FinishProcessing(notif);
    }
    catch (Exception ex)
    {
      _logger.LogError("Handling StripeSubsChange Failed for notif with notifId {notifId} with error {error}", stripeSubsChange.NotifId, ex.Message);
      notif.ProcessingStatus = ProcessingStatus.Failed;
      await _context.SaveChangesAsync();
      throw;
    }
  }
}
