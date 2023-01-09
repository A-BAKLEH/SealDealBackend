using SharedKernel.DomainEvents;

namespace Core.Domain.AgencyAggregate.Events;
public class AgencySignedUpEvent : DomainEventBase
{
  public int AgencyId { get; }
  public AgencySignedUpEvent(int agencyId)
  {
    AgencyId = agencyId;
  }
}
