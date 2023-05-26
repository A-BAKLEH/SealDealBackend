using Core.Domain.ActionPlanAggregate;

namespace Core.DTOs.ProcessingDTOs;
public class ActionPlanAssociationDTO
{
    public int Id { get; set; }
    public string APName { get; set; }
    public int APID { get; set; }
    public string APStatus { get; set; }
}
