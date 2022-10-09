
using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.Core.Domain.NotificationAggregate;

namespace Clean.Architecture.Core.DTOs.ProcessingDTOs;
public class WrapperNotifDashboard
{
  public DashboardLeadDTO? leadDTO { get; set; }
  public IEnumerable<NotifForDashboardDTO> notifs {get; set;}
  public int leadId { get; set; }
}
