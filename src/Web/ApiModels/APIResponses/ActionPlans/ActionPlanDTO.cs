namespace Web.ApiModels.APIResponses.ActionPlans;

public class ActionPlanDTO
{
  public int id { get; set; }
  public string name { get; set; }
  public bool isActive { get; set; }
  public string Trigger { get; set; }
  public bool StopPlanOnInteraction { get; set; }
  public string? FirstActionDelay { get; set; }
  public DateTime TimeCreated { get; set; }
  public int ActionsCount { get; set; }
  public List<ActionDTO> Actions { get; set; }
}

public class ActionDTO
{
  public int ActionLevel { get; set; }
  public Dictionary<string, string> ActionProperties { get; set; }
  public string? NextActionDelay { get; set; }
}
