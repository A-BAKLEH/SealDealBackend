using Clean.Architecture.Core.DTOs.ProcessingDTOs;
namespace Clean.Architecture.Web.ApiModels.APIResponses.Templates;

public class AllTemplatesDTO
{
  public List<EmailTemplateDTO> emailTemplates { get; set; }
  public List<SmsTemplateDTO> smsTemplates { get; set; }
}
