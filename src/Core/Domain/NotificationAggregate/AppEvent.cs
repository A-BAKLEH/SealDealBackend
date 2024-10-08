﻿using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using SharedKernel;

namespace Core.Domain.NotificationAggregate;

public enum ProcessingStatus { NoNeed, Scheduled, Failed, WaitingInBatch, Done }
/// <summary>
/// Represents an event that happened in the app, always related to a broker but can also relate to lead
/// </summary>

public class AppEvent : Entity<int>
{
    public Guid BrokerId { get; set; }
    public Broker Broker { get; set; }
    public int? LeadId { get; set; }
    public Lead? lead { get; set; }
    public DateTime EventTimeStamp { get; set; }
    public EventType EventType { get; set; }
    /// <summary>
    /// default false
    /// </summary>
    public bool ReadByBroker { get; set; } = false;
    /// <summary>
    /// default true. True when visibe to broker in the app and will be notified of. false if backend-event only
    /// </summary>
    public bool NotifyBroker { get; set; } = true;
    /// <summary>
    /// Delete if true after its processed
    /// </summary>
    public bool DeleteAfterProcessing { get; set; } = false;
    /// <summary>
    /// true if the event reflects an action done by an action plan, or related to action plan
    /// used to filter notifs and find result of a given actionPlan's action on a lead
    /// and similar queries
    /// </summary>
    public bool IsActionPlanResult { get; set; } = false;
    public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.NoNeed;
    public Dictionary<string, string> Props { get; set; } = new();
}

[Flags]
public enum EventType
{
    None = 0,
    /// <summary>
    /// data: old status, new status, if IsActionPlanResult:  ActionPlanId,ActionPlanName, ActionId,APAssID
    /// process: signalR/push notif
    /// </summary>                                           
    LeadStatusChange = 1 << 0, //1
    /// <summary>                                         
    /// data: listing Id, listing address name or Id of actor who did it 
    /// </summary>
    ListingAssigned = 1 << 1, //2

    /// <summary>
    /// listing Id, listingaddress name or Id of actor who did it
    /// </summary>
    ListingUnAssigned = 1 << 2, //4

    /// <summary>
    /// whenever UNASSIGNED Lead enters the system no matter how. BrokerId is the Id of person who created the Lead.
    /// 
    /// data: if automatically created, EmailId 
    /// </summary>
    LeadCreated = 1 << 3, // 8

    /// <summary>
    /// whenever an association between lead and me as a broker is created, BrokerId is the Id of Broker
    /// It is assigned to.
    /// data: UserId who did it, message/comment from person who assigned it if not broker to himself.
    /// </summary>
    LeadAssignedToYou = 1 << 4, // 16

    /// <summary>
    /// for admin: when admin assigns a lead to someone else
    /// </summary>
    YouAssignedtoBroker = 1 << 5, // 32
    /// <summary>
    /// will only be possible after admin manually assigns lead to a broker
    /// </summary> ******DELETED NOT USED ************
    //LeadUnAssigned = 1 << 5,

    /// <summary>
    /// data: props : APTriggerType, ActionPlanId, ActionPlanName
    /// </summary>
    ActionPlanStarted = 1 << 6, //64

    /// <summary>
    /// data: props :  ActionPlanId, APFinishedReason, ActionPlanName
    /// </summary>
    ActionPlanFinished = 1 << 7, //128

    ActionPlanEmailSent = 1 << 8, //256

    /// <summary>
    /// data: UserId who createed it ,TempPassword, EmailSent?
    /// </summary>
    BrokerCreated = 1 << 9, //512
    StripeSubsChanged = 1 << 10, //1024

    //Analyzer Notifs types-----------

    /// <summary>
    /// email from lead unseen for > 1 hour
    /// </summary>
    UnSeenEmail = 1 << 20,   //1048576
    /// <summary>
    /// Seen && ReplyNeeded && Unreplied-to emails from leads for 2 > hours
    /// </summary>
    UnrepliedEmail = 1 << 21,
    /// <summary>
    /// Unseen NewLead appEvent for > 15 mins
    /// </summary>
    UnseenNewLead = 1 << 22,  //4194304

    /// <summary>
    /// for admin when manually/automation creates a lead and lead is unassigned for > 1 hour
    /// </summary>
    UnAssignedLead = 1 << 23
}
