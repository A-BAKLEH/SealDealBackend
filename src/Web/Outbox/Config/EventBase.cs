using MediatR;

namespace Web.Outbox.Config;

public abstract class EventBase : INotification
{
  public int AppEventId { get; set; }
}
