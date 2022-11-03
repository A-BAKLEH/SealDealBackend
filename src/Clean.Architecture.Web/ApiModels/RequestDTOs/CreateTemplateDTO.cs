namespace Clean.Architecture.Web.ApiModels.RequestDTOs;

public class CreateTemplateDTO
{
  /// <summary>
  /// "e" for email, "s" for sms
  /// </summary>
  public string TemplateType { get; set; }
  /// <summary>
  /// subject for email
  /// </summary>
  public string? subject { get; set; }
  public string text { get; set; }
  /// <summary>
  /// displayed name
  /// </summary>
  public string TemplateName { get; set; }
}
