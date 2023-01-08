using Clean.Architecture.Core.DTOs.ProcessingDTOs;
namespace Clean.Architecture.Web.ApiModels.APIResponses.Templates;

public class AllTemplatesDTO
{
  public List<TemplateDTO> allTemplates { get; set; } = new();
}
