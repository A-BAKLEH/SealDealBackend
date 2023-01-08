using Clean.Architecture.Core.Domain.NotificationAggregate;

namespace Clean.Architecture.Core.DTOs.ProcessingDTOs;
public class NotifForDashboardDTO
{
  public NotifType NotifType { get; set; }

  public bool ReadByBroker { get; set; }
  public DateTimeOffset UnderlyingEventTimeStamp { get; set; }

}
