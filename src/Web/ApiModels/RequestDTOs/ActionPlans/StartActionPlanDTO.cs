namespace Web.ApiModels.RequestDTOs.ActionPlans;

public class StartActionPlanDTO
{
  public int ActionPlanID { get; set; }
  public int LeadId { get; set; }
  /// <summary>
  /// leave null if you want the action plan's default delay
  /// format: 00/00/00 days hours seconds
  /// </summary>
  public string? customDelay { get; set; }
}
