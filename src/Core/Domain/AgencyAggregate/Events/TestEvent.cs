using SharedKernel.DomainEvents;

namespace Core.Domain.AgencyAggregate.Events;
public class TestEvent : DomainEventBase
{
  public int AgencyId { get; init; }
}
