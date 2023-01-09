

namespace Core.Domain.NotificationAggregate.HelperObjects;
public class ReceivedEmailData : AbstractEmailData
{
  public string SenderLeadEmail { get; set; }
  public int SenderLeadId { get; set; }
}
