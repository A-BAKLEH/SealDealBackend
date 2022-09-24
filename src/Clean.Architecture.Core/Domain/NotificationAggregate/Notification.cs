using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.NotificationAggregate;
public class Notification :Entity<int>
{
  public Guid BrokerId { get; set; }
  public int? LeadId { get; set; }
  public DateTime NotifCreatedAt { get; set; }
  public NotifType NotifType { get; set; }
  public bool ReadByBroker { get; set; } = false;
  public bool NotifyBroker { get; set; }
  public NotifHandlingStatus NotifHandlingStatus { get; set; } = NotifHandlingStatus.UnHandled;
  /// <summary>
  /// JSON string:string format only for now. later can create cutsom serializer
  /// </summary>
  public Dictionary<string, string> NotifData { get; set; } = new();
}
public enum NotifHandlingStatus { Success, Error,UnHandled}

[Flags]
public enum NotifType
{
  EmailSent = 1, EmailReceived = 2, SmsSent= 4,
  SmsReceived =8, Call =16, LeadStatusChange =32,
  ListingAssigned = 64, LeadAssigned = 128, LeadManuallyAdded = 256,
}
