
namespace Clean.Architecture.Core.Domain.NotificationAggregate.HelperObjects;
public class SentEmailData : AbstractEmailData
{
  public string ReceiverLeadEmail { get; set; }
  public int ReceiverLeadId { get; set; }
}


