namespace Core.DTOs.ProcessingDTOs;

public class ActionPlanStartDTO
{
    public List<int> errorIDs { get; set; }
    public List<int> AlreadyRunningIDs { get; set; }
}
