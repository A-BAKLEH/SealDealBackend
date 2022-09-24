

using Clean.Architecture.Core.Domain.LeadAggregate;

namespace Clean.Architecture.Core.Domain.NotificationAggregate.Events;
public class LeadStatusChangedEvent
{
  public LeadStatus PreviousStatus { get; set; }
  public LeadStatus CurrentStatus { get; set; }

  public DateTime StatusChangetime { get; set; }
  public string? StatusChangeReason { get; set; }
}
