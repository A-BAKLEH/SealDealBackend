
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;

namespace Core.DTOs.ProcessingDTOs;
public class WrapperNotifDashboard
{
  public DashboardLeadDTO? leadDTO { get; set; }
  public IEnumerable<NotifForDashboardDTO> notifs {get; set;}
  public int leadId { get; set; }
}
