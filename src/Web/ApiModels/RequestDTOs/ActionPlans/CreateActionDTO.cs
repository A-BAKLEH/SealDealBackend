using Microsoft.Build.Framework;

namespace Web.ApiModels.RequestDTOs.ActionPlans;

public class CreateActionDTO
{
  /// <summary>
  /// options : ChangeLeadStatus, SendEmail, SendSms
  /// </summary>
  [Required]
  public string ActionType { get; set; }

  /// <summary>
  /// start from 1 for 1st action and so on
  /// </summary>
  [Required]
  public int ActionLevel { get; set; }

  /// <summary>
  /// delay before executing next action, i.e. between the end of this action
  /// and beginning of next one
  /// format: Days:hours:minutes
  /// integer values only. If no delay required, input  "0:0:0"
  /// </summary>
  [Required]
  public string NextActionDelay { get; set; }

  //Email or Sms Template ID
  public int? TemplateId { get; set; }

  /// <summary>
  /// key value pairs
  /// If ChangeLeadStatus => "NewLeadStatus":New, Active, Client, Closed, Dead<para />
  /// for now <para/>
  /// </summary>
  public Dictionary<string, string> Properties { get; set; }
}
