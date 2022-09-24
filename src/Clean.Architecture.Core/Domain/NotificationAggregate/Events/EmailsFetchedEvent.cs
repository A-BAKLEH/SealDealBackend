
using Clean.Architecture.Core.Domain.NotificationAggregate.HelperObjects;

namespace Clean.Architecture.Core.Domain.NotificationAggregate.Events;
public class EmailsFetchedEvent
{
  public Guid BrokerId { get; set; }
  public List<SentEmailData> SentEmails {get; set;} = new();
  public List<ReceivedEmailData> ReceivedEmails { get; set; } = new();
}
