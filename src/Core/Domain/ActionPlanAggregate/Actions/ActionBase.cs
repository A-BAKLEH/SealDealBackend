
using SharedKernel;

namespace Core.Domain.ActionPlanAggregate.Actions;
public abstract class ActionBase: Entity<int>
{
  public int ActionPlanId { get; set; }
  public ActionPlan ActionPlan { get; set; }
  /// <summary>
  /// starts at 1 for first action in action plan
  /// </summary>
  public int ActionLevel { get; set; }

  /// <summary>
  /// NON instance-specific properties like
  /// emailtemplateId, sms template Id, ChangeStatusToX, NoteToAddToLead, etc
  /// Strictly string:string dictionary for now
  /// each derived Action Class will know what keys map to what values
  /// </summary>
  public Dictionary<string, string> ActionProperties { get; set; } = new();
  public List<ActionTracker> ActionTrackers { get; set; }

  /// <summary>
  /// delay before executing next action
  /// format: Days:hours:minutes 
  /// integer values only
  /// </summary>
  public string? NextActionDelay { get; set; }
  /// <summary>
  /// returns Tuple
  /// T1:ActionResultId for sentEmail for example and T2: string for info
  /// </summary>
  /// <returns></returns>
  public abstract Task<Tuple<int?, string?>> Execute();
}
