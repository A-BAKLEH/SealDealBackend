

namespace Clean.Architecture.Core.Domain.NotificationAggregate.HelperObjects;
public class SentSmsData : AbstractSmsData
{
  public string ReceiverLeadNumber { get; set; }
  public int ReceiverLeadId { get; set; }
}
