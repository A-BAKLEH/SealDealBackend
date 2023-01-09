using Core.DTOs.ProcessingDTOs;
namespace Web.ApiModels.APIResponses.Templates;

public class AllTemplatesDTO
{
  public List<TemplateDTO> allTemplates { get; set; } = new();
}
