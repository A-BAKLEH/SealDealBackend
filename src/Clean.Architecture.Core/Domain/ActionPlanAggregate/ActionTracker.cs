using Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions;
using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.ActionPlanAggregate;
/// <summary>
/// contains Instance-Specific properties about tracked action
/// </summary>
public class ActionTracker : EntityBase
{
  public int TrackedActionId { get; set;}
  public ActionBase TrackedAction { get; set; }
  public int ActionPlanAssociationId { get; set;}
  public ActionPlanAssociation ActionPlanAssociation { get; set; }
  public ActionStatus ActionStatus { get; set; }
  /// <summary>
  /// hangfire job that will execute THIS tracked action, not the one after the delay
  /// </summary>
  public string? HangfireJobId { get; set; }
  /// <summary>
  /// if null immediate
  /// </summary>
  public DateTimeOffset? HangfireScheduledStartTime { get; set; }
  public DateTimeOffset? ExecutionCompletedTime { get; set; }
  /// <summary>
  /// Details about failures or other relevant Status Info
  /// </summary>
  public string? ActionStatusInfo { get; set; }
  /// <summary>
  /// can be the sent emailId, sent smsId, Some action result that should be tracked
  /// </summary>
  public int? ActionResultId { get; set; }
}
public enum ActionStatus
{
  Done, Failed, ScheduledToStart, Overdue, CancelledByLeadResponse
}
