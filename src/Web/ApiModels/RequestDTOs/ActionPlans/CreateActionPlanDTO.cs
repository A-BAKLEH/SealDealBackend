using System.ComponentModel.DataAnnotations;

namespace Web.ApiModels.RequestDTOs.ActionPlans;

public class CreateActionPlanDTO
{
  public string Name { get; set; }

  /// <summary>
  /// options : LeadAssigned, Manual
  /// </summary>
  [Required(AllowEmptyStrings = false)]
  public string Trigger { get; set; }

  [Required]
  public bool StopPlanOnInteraction { get; set; }

  /// <summary>
  /// if True, triggers will start triggering lal sebe7 directly if actionplan
  /// has an automatic trigger
  /// </summary>
  [Required]
  public bool ActivateNow { get; set; }

  /// <summary>
  /// optional
  /// delay before executing first action
  /// format: Days:hours:minutes 
  /// integer values only
  /// for example when a new lead enters, first email sending will be delayed
  /// by x minutes.
  /// </summary>
  public string? FirstActionDelay { get; set; }

  [Required]
  public List<CreateActionDTO> Actions { get; set; }
}
