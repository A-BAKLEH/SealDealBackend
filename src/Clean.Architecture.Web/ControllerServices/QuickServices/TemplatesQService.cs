using Clean.Architecture.Core.Constants.ProblemDetailsTitles;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.DTOs.ProcessingDTOs;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.SharedKernel.Exceptions;
using Clean.Architecture.Web.ApiModels.APIResponses.Templates;
using Clean.Architecture.Web.ApiModels.RequestDTOs;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Web.ControllerServices.QuickServices;

public class TemplatesQService
{
  private readonly AppDbContext _appDbContext;

  public TemplatesQService(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }

  public async Task<TemplateDTO> CreateTemplateAsync(CreateTemplateDTO dto, Guid brokerId)
  {
    if(_appDbContext.Templates.Any(t => t.BrokerId == brokerId && t.Title == dto.TemplateName))
    {
      throw new CustomBadRequestException($"template already exists with name '{dto.TemplateName}'", ProblemDetailsTitles.AlreadyExists);
    }
    Template template;
    if (dto.TemplateType == "e")
    {
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
    /*var Emailtemplates = await _appDbContext.EmailTemplates
      .OrderByDescending(t => t.Modified)
      .Where(t => t.BrokerId == brokerId)
      .Select(x => new EmailTemplateDTO
      {
        id = x.Id,
        Modified = x.Modified,
        subject = x.EmailTemplateSubject,
        templateText = x.templateText,
        TimesUsed = x.TimesUsed,
        Title = x.Title,
      })
      .ToListAsync();
    var Smstemplates = await _appDbContext.SmsTemplates
      .OrderByDescending(t => t.Modified)
      .Where(t => t.BrokerId == brokerId)
      .Select(x => new SmsTemplateDTO
      {
        id = x.Id,
        Modified = x.Modified,
        templateText = x.templateText,
        TimesUsed = x.TimesUsed,
        Title= x.Title,
      })
      .ToListAsync();*/
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
