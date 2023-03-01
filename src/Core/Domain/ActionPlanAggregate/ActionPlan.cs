
using Core.Domain.ActionPlanAggregate.Actions;
using Core.Domain.BrokerAggregate;
using Core.Domain.NotificationAggregate;
using SharedKernel;

namespace Core.Domain.ActionPlanAggregate;
public class ActionPlan : Entity<int>
{
  //Automatic triggers , if manual triggering only then NotifType.None. All action plans
  //with automatic triggers can be started manually too for a given lead
  public NotifType Triggers { get; set; }
  /// <summary>
  /// Notifs that affect ActionPlan AFTER its is triggered.For now will be lead interaction notifs
  /// combined flag IFF StopPlanOnInteraction == true
  /// which are the notifs to listen to to stop plan execution
  /// </summary>
  public NotifType NotifsToListenTo { get; set; } = NotifType.None;
  public Guid BrokerId { get; set; }
  public Broker broker { get; set; }
  /// <summary>
  /// client timeZ
  /// </summary>
  public DateTimeOffset TimeCreated { get; set; }

  /// <summary>
  /// only relevant for action plans that have automatic triggering
  /// </summary>
  public bool isActive { get; set; }
  
  public int ActionsCount { get; set; }
  public string Name { get; set; }
  public bool StopPlanOnInteraction { get; set; }
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
  public List<ActionBase> Actions { get; set; }
  public List<ActionPlanAssociation> ActionPlanAssociations { get; set; }
  
}
