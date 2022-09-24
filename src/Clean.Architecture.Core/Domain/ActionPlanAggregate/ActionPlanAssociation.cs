
using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Repositories;

namespace Clean.Architecture.Core.Domain.ActionPlanAggregate;
public class ActionPlanAssociation: Entity<int>
{
  /// <summary>
  /// overrides ActionPlan's "FirstActionDelay" if not null
  /// used to specify a delay for this lead only
  /// </summary>
  public string? CustomDelay { get; set; }
  public int? ActionPlanId { get; set; }
  public ActionPlan ActionPlan { get; set; }
  /// <summary>
  /// null if triggered manually
  /// </summary>
  public int? TriggerNotificationId { get; set; }
  public int LeadId { get; set; }
  public Lead lead { get; set; }
  public DateTime ActionPlanTriggeredAt { get; set; } = DateTime.UtcNow;
  /// <summary>
  /// Status of this instance of the associated action plan that belongs to this specific lead
  /// </summary>
  public ActionPlanStatus ThisActionPlanStatus { get; set; }
  public List<ActionTracker> ActionTrackers { get; set; }
  /// <summary>
  /// If null no action has been scheduled yet
  /// Updated whenever an action execution completes, when scheduling next action with delay, the new delayed actionId is inserted here
  /// </summary>
  public int? currentTrackedActionId { get; set; }
  public string? FirstActionHangfireId { get; set; }
}

public enum ActionPlanStatus
{ Cancelled,ErrorStopped, Paused, Running, Done }

