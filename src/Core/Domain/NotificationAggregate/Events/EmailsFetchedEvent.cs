
using Core.Domain.NotificationAggregate.HelperObjects;
using SharedKernel.DomainEvents;

namespace Core.Domain.NotificationAggregate.Events;
public class EmailsFetchedEvent:DomainEventBase
{
  public Guid BrokerId { get; set; }
  public List<SentEmailData> SentEmails {get; set;} = new();
  public List<ReceivedEmailData> ReceivedEmails { get; set; } = new();
}
