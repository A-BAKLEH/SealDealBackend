using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.NotificationAggregate;
public class Notification : Entity<int>
{
  //brokerNotifs : todoTask soon, listing assigned,
  public Guid BrokerId { get; set; }
  public int? LeadId { get; set; }
  public DateTime NotifCreatedAt { get; set; }
  public DateTime UnderlyingEventTimestamp { get; set; }
  public NotifType NotifType { get; set; }
  public bool ReadByBroker { get; set; } = false;
  /// <summary>
  /// when set to true, notif will always get fetched until its seen, even if its been a while since it was created. 
  /// This will happen
  /// </summary>
  public bool NotifyBroker { get; set; }
  public NotifHandlingStatus NotifHandlingStatus { get; set; } = NotifHandlingStatus.UnHandled;
  /// <summary>
  /// JSON string:string format only for now. later can create cutsom serializer
  /// </summary>
  public Dictionary<string, string> NotifProps { get; set; } = new();
  /// <summary>
  /// sms or email text
  /// </summary>
  public string? NotifData { get; set; }
  public string? BrokerComment { get; set; }
}
public enum NotifHandlingStatus { Success, Error,UnHandled}

[Flags]
public enum NotifType
{
  /// <summary>
  /// data: subject, Thread/Convo Id, Email text, Email TimeStamp, EmailId
  /// </summary>
  EmailSent = 1,
  /// <summary>
  /// data: subject, Thread/Convo Id, Email text, Email TimeStamp, EmailId, EmailRead?, EmailReplied?
  /// </summary>
  EmailReceived = 2,
  /// <summary>
  /// data: text, timestamp
  /// </summary>
  SmsSent = 4,
  /// <summary>
  /// data: text, timestamp, Replied?
  /// </summary>
  SmsReceived = 8,
  /// <summary>
  /// data: duration, startTime, Call Missed, answered. Caller, Callee
  /// </summary>
  Call = 16,
  /// <summary>
  /// data: old status, new status, timestamp, message (who changed the status , maybe reason)
  /// </summary>
  LeadStatusChange = 32,
  /// <summary>
  /// data: 
  /// </summary>
  ListingAssigned = 64,
  /// <summary>
  /// data: who added lead, source
  /// </summary>
  LeadAssigned = 128,
  /// <summary>
  /// data: triggger
  /// </summary>
  ActionPlanStarted = 256,
}
