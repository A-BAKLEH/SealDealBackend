using MediatR;
using Web.Outbox.Config;

namespace Web.Outbox;

public class StripeSubsChange : EventBase
{
}

public class StripeSubsChangeHandler : INotificationHandler<StripeSubsChange>
{
  public async Task Handle(StripeSubsChange notification, CancellationToken cancellationToken)
  {
    Console.WriteLine("from handler:" + notification.NotifId);
  }
}
