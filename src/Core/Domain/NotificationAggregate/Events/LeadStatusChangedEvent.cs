

using Core.Domain.LeadAggregate;
using SharedKernel.DomainEvents;

namespace Core.Domain.NotificationAggregate.Events;
public class LeadStatusChangedEvent:DomainEventBase
{
  public LeadStatus PreviousStatus { get; set; }
  public LeadStatus CurrentStatus { get; set; }

  public DateTime StatusChangetime { get; set; }
  public string? StatusChangeReason { get; set; }
}
