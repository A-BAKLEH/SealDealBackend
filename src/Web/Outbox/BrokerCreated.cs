using MediatR;
using Web.Outbox.Config;

namespace Web.Outbox;

public class BrokerCreated : EventBase
{

}

public class BrokerCreatedHandler : INotificationHandler<BrokerCreated>
{
  public async Task Handle(BrokerCreated notification, CancellationToken cancellationToken)
  {
    Console.WriteLine("from handler:" + notification.NotifId);
  }
}
