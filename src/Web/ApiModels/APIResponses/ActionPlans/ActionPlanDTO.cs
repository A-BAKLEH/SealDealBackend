using Core.Domain.NotificationAggregate;

namespace Web.ApiModels.APIResponses.ActionPlans;

public class ActionPlanDTO
{
    public int id { get; set; }
    public string name { get; set; }
    public Guid brokerId { get; set; }
    public bool isActive { get; set; }
    public EventType FlagTriggers { get; set; }
    public List<string> Triggers { get; set; }
    public bool StopPlanOnInteraction { get; set; }
    public string? FirstActionDelay { get; set; }
    public DateTime TimeCreated { get; set; }
    public int ActionsCount { get; set; }
    public int ActiveOnXLeads { get; set; }
    public IEnumerable<LeadNameIdDTO> leads { get; set; }
    public IEnumerable<ActionDTO> Actions { get; set; }

}

public class ActionDTO
{
    public string actionType { get; set; }
    public int ActionLevel { get; set; }
    public int? TemplateId { get; set; }
    public Dictionary<string, string> ActionProperties { get; set; }
    public string? NextActionDelay { get; set; }
}

public class LeadNameIdDTO
{
    public string firstName { get; set; }
    public string lastName { get; set; }
    public int LeadId { get; set; }
}
