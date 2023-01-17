using Core.Domain.LeadAggregate;
using SharedKernel;

namespace Core.Domain.NotificationAggregate;

public enum APHandlingStatus{ Handled, Scheduled, Failed}

public enum ProcessingStatus { NoNeed,Scheduled, Failed,Done }
/// <summary>
/// Represents an event that happened in the app, always related to a broker but can also relate to lead
/// </summary>

public class Notification : Entity<int>
{
  public Guid BrokerId { get; set; }
  public int? LeadId { get; set; }
  public Lead? lead { get; set; }

  /// <summary>
  /// when 3rd-party event this always reflects its time
  /// </summary>
  public DateTimeOffset EventTimeStamp { get; set; }
  public NotifType NotifType { get; set; }

  /// <summary>
  /// true if its an interaction that was initiated by Lead, false if broker initiated it, null if not an interaction
  /// </summary>
  public bool? IsRecevied { get; set; }
  /// <summary>
  /// For in-app events: straightforward
  /// For lead interactions: Read either in SealDeal or in email client / mobile Sms app , call Answered 
  /// when false: email Unread, Sms not replied to/ unread(depends on how easy to check on phone),
  /// call missed 
  /// </summary>
  public bool ReadByBroker { get; set; }

  /// <summary>
  /// set to true to keep reminding el hmar to check it out until ReadByBroker is true
  /// true if notifType: sms received, 
  /// </summary>
  public bool NotifyBroker { get; set; }

  //Processing Part--------------------------

  /// <summary>
  /// Delete if true after its processed
  /// </summary>
  public bool DeleteAfterProcessing { get; set; } = false;

  /// <summary>
  /// when has to be handled by an action plan
  /// </summary>
  public APHandlingStatus? APHandlingStatus { get; set; }

  /// <summary>
  /// When Outbox Handler has to process the Notif
  /// </summary>
  public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.NoNeed;

  /// <summary>
  /// JSON string:string format only for now. later can create cutsom serializer
  /// sms text for sms,
  /// "call duration,broker comment" for call
  /// email id for email
  /// </summary>
  public Dictionary<string, string> NotifProps { get; set; } = new();

}

[Flags]
public enum NotifType
{
  None = 0,
  EmailEvent = 1 << 0,
  SmsEvent = 1 << 1,
  CallEvent = 1 << 2,
  /// <summary>
  /// for calls from lead that broker missed
  /// </summary>
  CallMissed = 1 << 3,
  /// <summary>
  /// data: old status, new status, reason? or action planId who did it
  /// </summary>
  LeadStatusChange = 1 << 4,
  /// <summary>
  /// data: listing Id, name or Id of actor who did it
  /// </summary>
  ListingAssigned = 1 << 5,

  /// <summary>
  /// listing Id, name or Id of actor who did it
  /// </summary>
  ListingUnAssigned = 1 << 6,
  /// <summary>
  /// also means lead created
  /// data: who created Lead (automatic, broker name (your name), admin or specific admin name ),
  /// source name if came from website
  /// </summary>
  LeadAssigned = 1 << 7,
  /// <summary>
  /// will only be possible after admin manually assigns lead to a broker
  /// data: lead Id
  /// </summary>
  LeadUnAssigned = 1 << 8,

  /// <summary>
  /// data: triggger, response event id
  /// </summary>
  ActionPlanStarted = 1 << 9,
  ActionPlanFinished = 1 << 10,

  /// <summary>
  /// data: UserId who createed it ,TempPassword, EmailSent?
  /// </summary>
  BrokerCreated = 1 << 11,
  StripeSubsChanged = 1 << 12,
}
