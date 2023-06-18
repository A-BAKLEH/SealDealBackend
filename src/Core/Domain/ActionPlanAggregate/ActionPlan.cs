using Core.Domain.BrokerAggregate;
using Core.Domain.NotificationAggregate;
using SharedKernel;

namespace Core.Domain.ActionPlanAggregate;
public class ActionPlan : Entity<int>
{
    //Automatic triggers , if manual triggering only then NotifType.None. All action plans
    //with automatic triggers can be started manually too for a given lead
    public EventType Triggers { get; set; }
    /// <summary>
    /// Events Other than calls, Emails, SMS that can intract with action Plan, like stop it.
    /// Depending on type of plan, for now would apply mostly to leads ListenToNotifs.
    /// FOR NOW NOT USED, just stop action plan when lead interacts. Later will have to add the logic 
    /// to handle it
    /// </summary>
    public EventType EventsToListenTo { get; set; } = EventType.None;
    public bool StopPlanOnInteraction { get; set; }
    public Guid BrokerId { get; set; }
    public Broker broker { get; set; }
    /// <summary>
    /// client timeZ
    /// </summary>
    public DateTime TimeCreated { get; set; }

    /// <summary>
    /// means running
    /// </summary>
    public bool isActive { get; set; }
    public byte ActionsCount { get; set; }
    public string Name { get; set; }
    /// <summary>
    /// always true for now - no system-level action plans
    /// </summary>
    public bool AssignToLead { get; set; } = true;

    /// <summary>
    /// delay before executing first action
    /// format: Days:hours:minutes 
    /// integer values only
    /// can be overriden for a spceific lead with APAssociation's Custom Delay
    /// </summary>
    public string? FirstActionDelay { get; set; }
    public int TimesUsed { get; set; } = 0;
    public int TimesSuccess { get; set; } = 0;
    public List<ActionPlanAction> Actions { get; set; }
    public List<ActionPlanAssociation> ActionPlanAssociations { get; set; }

}
