using Core.Domain.ActionPlanAggregate;

namespace Core.DTOs.ProcessingDTOs;

public class ActionExecutingDTO
{
    public byte ActionLevel { get; set; }
    public int Id { get; set; }
    public Dictionary<string, string> ActionProperties { get; set; }
    public ActionType ActionType { get; set; }
    public Guid BrokerId { get; set; }
    public int? dataTemplateId { get; set; }
    public string? nextActionDelay { get; set; }
}
