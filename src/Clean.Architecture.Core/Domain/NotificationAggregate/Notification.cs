using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.NotificationAggregate;
/// <summary>
/// Represents an event that happened in the app
/// </summary>
public class Notification : Entity<int>
{
  public Guid BrokerId { get; set; }
  public int? LeadId { get; set; }
  public Lead? lead { get; set; }


  public DateTime NotifCreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UnderlyingEventTimeStamp { get; set; }


  public NotifType NotifType { get; set; }

  /// <summary>
  /// For lead interactions: Read either in SealDeal or in email client / mobile Sms app , call Answered 
  /// when false: email Unread, Sms not replied to/ unread(depends on how easy to check on phone), call missed 
  /// </summary>
  public bool ReadByBroker { get; set; } = false;
  /// <summary>
  /// set to true to keep reminding el hmar to check it out until ReadByBroker is true
  /// true if notifType: sms received, 
  /// </summary>
  public bool NotifyBroker { get; set; }
  /// <summary>
  /// Action Plans handling status
  /// </summary>
  public NotifHandlingStatus NotifHandlingStatus { get; set; } = NotifHandlingStatus.UnHandled;
  /// <summary>
  /// JSON string:string format only for now. later can create cutsom serializer
  /// props related to the resource like for email : subject, CCS, threadId
  /// </summary>
  public Dictionary<string, string> NotifProps { get; set; } = new();
  /// <summary>
  /// sms or email text
  /// </summary>
  public string? NotifData { get; set; }
  /// <summary>
  /// comment manually inserted by broker on the event
  /// </summary>
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
  CallReceived = 16,
  CallSent = 32,
  /// <summary>
  /// data: old status, new status, timestamp, message (who changed the status , maybe reason)
  /// </summary>
  LeadStatusChange = 64,
  /// <summary>
  /// data: 
  /// </summary>
  ListingAssigned = 128,
  /// <summary>
  /// data: who added lead, source
  /// </summary>
  LeadAssigned = 256,
  /// <summary>
  /// data: triggger
  /// </summary>
  ActionPlanStarted = 512,
}
