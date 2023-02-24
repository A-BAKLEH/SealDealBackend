using System.ComponentModel.DataAnnotations;

namespace Web.ApiModels.RequestDTOs;

public class UpdateTemplateDTO
{
  // you can leave unchanged proprety null
  public int TemplateId { get; set; }
  /// <summary>
  /// "e" for email, "s" for sms
  /// </summary>
  ///
  [Required(AllowEmptyStrings = false)]
  public string TemplateType { get; set; }
  /// <summary>
  /// subject for email
  /// </summary>
  public string? subject { get; set; }
  public string? text { get; set; }
  /// <summary>
  /// displayed name
  /// </summary>
  public string? TemplateName { get; set; }
}
