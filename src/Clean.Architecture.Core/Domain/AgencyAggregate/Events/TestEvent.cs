using Clean.Architecture.SharedKernel.DomainEvents;

namespace Clean.Architecture.Core.Domain.AgencyAggregate.Events;
public class TestEvent : DomainEventBase
{
  public int AgencyId { get; init; }
}
