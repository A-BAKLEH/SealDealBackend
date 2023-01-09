using Core.Constants.ProblemDetailsTitles;
using Core.Domain.BrokerAggregate.Templates;
using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using SharedKernel.Exceptions;
using Web.ApiModels.APIResponses.Templates;
using Web.ApiModels.RequestDTOs;
using Microsoft.EntityFrameworkCore;

namespace Web.ControllerServices.QuickServices;

public class TemplatesQService
{
  private readonly AppDbContext _appDbContext;

  public TemplatesQService(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }

  public async Task<TemplateDTO> CreateTemplateAsync(CreateTemplateDTO dto, Guid brokerId)
  {
    if (_appDbContext.Templates.Any(t => t.BrokerId == brokerId && t.Title == dto.TemplateName))
    {
      throw new CustomBadRequestException($"template already exists with name '{dto.TemplateName}'", ProblemDetailsTitles.AlreadyExists);
    }
    if (string.IsNullOrWhiteSpace(dto.text) || string.IsNullOrWhiteSpace(dto.TemplateName)
      || string.IsNullOrWhiteSpace(dto.TemplateName) || string.IsNullOrWhiteSpace(dto.TemplateType))
    {
      throw new CustomBadRequestException("empty input", ProblemDetailsTitles.EmptyInput);
    }
    Template template;
    if (dto.TemplateType == "e")
    {
      if (string.IsNullOrWhiteSpace(dto.subject)) throw new CustomBadRequestException("empty input", ProblemDetailsTitles.EmptyInput);
      template = new EmailTemplate
      {
        BrokerId = brokerId,
        EmailTemplateSubject = dto.subject,
        Modified = DateTime.UtcNow,
        templateText = dto.text,
        TimesUsed = 0,
        Title = dto.TemplateName
      };
    }
    else
    {
      template = new SmsTemplate
      {
        BrokerId = brokerId,
        Modified = DateTime.UtcNow,
        templateText = dto.text,
        TimesUsed = 0,
        Title = dto.TemplateName,
      };
    }
    _appDbContext.Templates.Add(template);
    await _appDbContext.SaveChangesAsync();
    //return template;
    return template.MapToDTO();
  }

  public async Task<AllTemplatesDTO> GetAllTemplatesAsync(Guid brokerId)
  {

    var templates = await _appDbContext.Templates
      .OrderByDescending(t => t.Modified)
      .Where(t => t.BrokerId == brokerId)
      .ToListAsync();
    AllTemplatesDTO allTemplatesDTO = new AllTemplatesDTO();
    foreach (var template in templates)
    {
      allTemplatesDTO.allTemplates.Add(template.MapToDTO());
    }

    return allTemplatesDTO;
  }
}
