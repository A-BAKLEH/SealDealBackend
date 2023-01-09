using SharedKernel.DomainEvents;

namespace Core.Domain.BrokerAggregate.Events;
public class BrokerSignedUpEvent : DomainEventBase
{
  public string brokerName { get; init; }
  public Guid brokerId { get; init; } 
}
