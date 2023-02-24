using Core.ExternalServiceInterfaces.ActionPlans;
using SharedKernel;

namespace Core.Domain.ActionPlanAggregate.Actions;
public abstract class ActionBase: Entity<int>
{
  public IActionExecuter? _IActionExecuter { get; set; }
  public int ActionPlanId { get; set; }
  public ActionPlan ActionPlan { get; set; }
  /// <summary>
  /// starts at 1 for first action in action plan
  /// </summary>
  public byte ActionLevel { get; set; }

  /// <summary>
  /// NON instance-specific properties like
  /// emailtemplateId, sms template Id, ChangeStatusToX, NoteToAddToLead, etc
  /// Strictly string:string dictionary for now
  /// each derived Action Class will know what keys map to what values
  /// </summary>
  public Dictionary<string, string> ActionProperties { get; set; } = new();
  public List<ActionTracker> ActionTrackers { get; set; }

  //used to store Id of template used by action or any other useful related data that can be
  //represented as an int
  public int? DataId { get; set; }

  /// <summary>
  /// delay before executing next action
  /// format: Days:hours:minutes
  /// integer values only
  /// </summary>
  public string? NextActionDelay { get; set; }

  public abstract Task<Tuple<bool,object?>> Execute(params Object[] pars);
}
