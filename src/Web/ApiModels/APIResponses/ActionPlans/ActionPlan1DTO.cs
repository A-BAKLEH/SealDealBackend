namespace Web.ApiModels.APIResponses.ActionPlans;

public class ActionPlan1DTO
{
  public int id { get; set; }
  public string name { get; set; }
  public bool isActive { get; set; }
  public string Trigger { get; set; }
  public bool StopPlanOnInteraction { get; set; }
  public string? FirstActionDelay { get; set; }
  public DateTime TimeCreated { get; set; }
  public int ActionsCount { get; set; }
  public List<Action1DTO> Actions { get; set; }
}

public class Action1DTO
{
  public int ActionLevel { get; set; }
  public int? TemplateId { get; set; }
  public Dictionary<string, string> ActionProperties { get; set; } = new();
  public string? NextActionDelay { get; set; }
}
