using Clean.Architecture.Core.Domain.ActionPlanAggregate;

namespace Clean.Architecture.Core.DTOs.ProcessingDTOs;
public class ActionPlanAssociationDTO
{
  public int Id { get; set; }
  public string APName { get; set; }
  public int APID { get; set; }
  public ActionPlanStatus APStatus { get; set; }

}
