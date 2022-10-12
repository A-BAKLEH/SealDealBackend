namespace Clean.Architecture.Web.ApiModels.RequestDTOs;

public class CreateToDoTaskDTO
{
  /// <summary>
  /// can be null
  /// </summary>
  public string? Description { get; set; }
  public string TaskName { get; set; }
  public DateTime dueTime { get; set; }
  public int? leadId { get; set; }

}
