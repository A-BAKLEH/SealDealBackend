
namespace Core.DTOs.ProcessingDTOs;
public class ToDoTaskWithLeadName
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public string TaskName { get; set; }
    public DateTime TaskDueDate { get; set; }
    public int? LeadId { get; set; }
    public string? firstName { get; set; }
    public string? lastName { get; set; }
  
}
