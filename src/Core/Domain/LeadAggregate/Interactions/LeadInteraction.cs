using Core.Domain.NotificationAggregate;
using SharedKernel;

namespace Core.Domain.LeadAggregate.Interactions;
public abstract class LeadInteraction : Entity<int>
{
  public int LeadId { get; set; }

  /// <summary>
  /// maybe remove
  /// </summary>
  public NotifType type { get; set; }

  /// <summary>
  /// true if lead initiated interaction
  /// </summary>
  public bool isReceived { get; set; }

  /// <summary>
  /// of underlying event
  /// </summary>
  public DateTimeOffset Timestamp { get; set; }

  /// <summary>
  /// Read for email, read for sms, went through (picked up) for call
  /// </summary>
  public bool isRead { set; get; }

  /// <summary>
  /// sms text for sms,
  /// "call duration,broker comment" for call =
  /// email id for email
  /// </summary>
  public string? data { get; set; }  


}
