namespace Clean.Architecture.Web.ApiModels.RequestDTOs;

public class CreateToDoTaskDTO
{
  public string taskText { get; set; }
  public DateTime dueTime { get; set; }
  public int? leadId { get; set; }

}
