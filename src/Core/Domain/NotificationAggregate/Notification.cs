using Core.Domain.LeadAggregate;
using SharedKernel;

namespace Core.Domain.NotificationAggregate;
/// <summary>
/// Represents an event that happened in the app, always related to a broker but can also relate to lead
/// </summary>
public class Notification : Entity<int>
{
  public Guid BrokerId { get; set; }
  public int? LeadId { get; set; }
  public Lead? lead { get; set; }
  public DateTimeOffset NotifCreatedAt { get; set; }
  public DateTimeOffset UnderlyingEventTimeStamp { get; set; }
  public NotifType NotifType { get; set; }
  /// <summary>
  /// For in-app events: straightforward
  /// For lead interactions: Read either in SealDeal or in email client / mobile Sms app , call Answered 
  /// when false: email Unread, Sms not replied to/ unread(depends on how easy to check on phone), call missed 
  /// </summary>
  public bool ReadByBroker { get; set; } = false;

  /// <summary>
  /// set to true to keep reminding el hmar to check it out until ReadByBroker is true
  /// true if notifType: sms received, 
  /// </summary>
  public bool NotifyBroker { get; set; }

  //----------- TODO maybe REMOVE -------
  /// <summary>
  /// Action Plans handling status
  /// </summary>
  public NotifHandlingStatus NotifHandlingStatus { get; set; } = NotifHandlingStatus.UnHandled;
  /// <summary>
  /// JSON string:string format only for now. later can create cutsom serializer
  /// props related to the resource like for email : subject, CCS, threadId
  /// can contain: brokercomment, other data
  /// </summary>
  public Dictionary<string, string> NotifProps { get; set; } = new();

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
  ListingUnAssigned = 256,
  /// <summary>
  /// also means lead created
  /// data: who created Lead (automatic, broker name (your name), admin or specific admin name ),
  /// source name if came from website
  /// </summary>
  LeadAssigned = 512,
  /// <summary>
  /// will only be possible after admin manually assigns lead to a broker
  /// </summary>
  LeadUnAssigned = 1024,
  /// <summary>
  /// data: triggger
  /// </summary>
  ActionPlanStarted = 2048,
  ActionPlanFinished = 4096,
}
