

namespace Core.Domain.NotificationAggregate.HelperObjects;
public class ReceivedSmsData : AbstractSmsData
{
  public string SenderLeadNumber { get; set; }
  public int SenderLeadId { get; set; }
}
