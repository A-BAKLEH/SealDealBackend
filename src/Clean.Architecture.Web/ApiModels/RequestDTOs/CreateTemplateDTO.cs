namespace Clean.Architecture.Web.ApiModels.RequestDTOs;

public class CreateTemplateDTO
{
  /// <summary>
  /// "e" for email, "s" for sms
  /// </summary>
  public string TemplateType { get; set; }
  public string? subject { get; set; }
  public string text { get; set; }
  public string TemplateName { get; set; }
}
