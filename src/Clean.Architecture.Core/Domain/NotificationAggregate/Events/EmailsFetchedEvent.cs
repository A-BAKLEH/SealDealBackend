
using Clean.Architecture.Core.Domain.NotificationAggregate.HelperObjects;
using Clean.Architecture.SharedKernel.DomainEvents;

namespace Clean.Architecture.Core.Domain.NotificationAggregate.Events;
public class EmailsFetchedEvent:DomainEventBase
{
  public Guid BrokerId { get; set; }
  public List<SentEmailData> SentEmails {get; set;} = new();
  public List<ReceivedEmailData> ReceivedEmails { get; set; } = new();
}
