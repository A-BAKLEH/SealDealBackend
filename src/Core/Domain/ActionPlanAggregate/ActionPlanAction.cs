using SharedKernel;

namespace Core.Domain.ActionPlanAggregate;
public enum ActionType
{
    ChangeLeadStatus, SendSms, SendEmail
}
public class ActionPlanAction : Entity<int>
{
    public const string NewLeadStatus = "NewLeadStatus";
    public int ActionPlanId { get; set; }
    public ActionPlan ActionPlan { get; set; }

    public ActionType ActionType { get; set; }
    /// <summary>
    /// starts at 1 for first action in action plan
    /// </summary>
    public byte ActionLevel { get; set; }

    /// <summary>
    /// NON instance-specific properties like
    /// ChangeLeadStatusToX, NoteToAddToLead, etc
    /// </summary>
    public Dictionary<string, string> ActionProperties { get; set; } = new();
    public List<ActionTracker> ActionTrackers { get; set; }

    //used to store Id of template used by action or any other int data that can be easily searchable
    public int? DataTemplateId { get; set; }

    /// <summary>
    /// delay before executing next action
    /// format: Days:hours:minutes
    /// integer values only
    /// </summary>
    public string? NextActionDelay { get; set; }
}
