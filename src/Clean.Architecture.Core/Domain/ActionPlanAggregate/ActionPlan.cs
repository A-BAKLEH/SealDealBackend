
using Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.NotificationAggregate;
using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.ActionPlanAggregate;
public class ActionPlan : Entity<int>
{
  //Triggers:  
  public NotifType Triggers { get; set; }
  /// <summary>
  /// for now will be lead interaction notifs combined flag IFF StopPlanOnInteraction == true
  /// which are the notifs to listen to to stop plan execution
  /// </summary>
  public NotifType NotifsToListenTo { get; set; }
  public Guid BrokerId { get; set; }
  public Broker broker { get; set; }
  public DateTime TimeCreated { get; set; } = DateTime.UtcNow;
  public bool isActive;
  public List<ActionBase> Actions { get; set; }
  public int ActionsCount { get; set; }
  public string Title { get; set; }
  public bool StopPlanOnInteraction { get; set; }
  /// <summary>
  /// always true for now - no system-level action plans
  /// </summary>
  public bool AssignToLead { get; set; } = true;
  public List<ActionPlanAssociation> ActionPlanAssociations { get; set; }
  /// <summary>
  /// delay before executing first action
  /// format: Days:hours:minutes 
  /// integer values only
  /// can be overriden for a spceific lead with APAssociation's Custom Delay
  /// </summary>
  public string? FirstActionDelay { get; set; }
}
