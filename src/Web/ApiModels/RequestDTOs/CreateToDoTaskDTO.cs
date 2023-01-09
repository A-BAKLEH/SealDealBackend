namespace Web.ApiModels.RequestDTOs;
using System.ComponentModel.DataAnnotations;

public class CreateToDoTaskDTO
{
  /// <summary>
  /// can be null
  /// </summary>
  public string? Description { get; set; }

  [Required(AllowEmptyStrings = false)]
  public string TaskName { get; set; }

  [Required]
  public DateTime dueTime { get; set; }
  public int? leadId { get; set; }
  public string? TempTimeZone { get; set; }

}
