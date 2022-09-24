
using Clean.Architecture.Core.Domain.NotificationAggregate.HelperObjects;

namespace Clean.Architecture.Core.Domain.NotificationAggregate.Events;
public class SmsFetchedEvent
{
  public Guid BrokerId { get; set; }
  public List<SentSmsData> SentSMS { get; set; } = new();
  public List<ReceivedSmsData> ReceivedSMS { get; set; } = new();
}
