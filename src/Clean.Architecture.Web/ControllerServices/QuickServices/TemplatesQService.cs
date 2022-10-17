using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.DTOs.ProcessingDTOs;
using Clean.Architecture.Infrastructure.Data;
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

  public async Task<Template> CreateTemplateAsync(CreateTemplateDTO dto, Guid brokerId)
  {
    Template template;
    if (dto.TemplateType == "e")
    {
      template = new EmailTemplate
      {
        BrokerId = brokerId,
        EmailTemplateSubject = dto.subject,
        Modified = DateTime.UtcNow,
        templateText = dto.text,
        TimesUsed = 0
      };
    }
    else
    {
      template = new SmsTemplate
      {
        BrokerId = brokerId,
        Modified = DateTime.UtcNow,
        templateText = dto.text,
        TimesUsed = 0
      };
    }
    _appDbContext.Templates.Add(template);
    await _appDbContext.SaveChangesAsync();
    return template;
  }

  public async Task<AllTemplatesDTO> GetAllTemplatesAsync(Guid brokerId)
  {
    var Emailtemplates = await _appDbContext.EmailTemplates
      .OrderByDescending(t => t.Modified)
      .Where(t => t.BrokerId == brokerId)
      .Select(x => new EmailTemplateDTO
      {
        id = x.Id,
        Modified = x.Modified,
        subject = x.EmailTemplateSubject,
        templateText = x.templateText,
        TimesUsed = x.TimesUsed,
      })
      .ToListAsync();
    var Smstemplates = await _appDbContext.SmsTemplates
      .OrderByDescending(t => t.Modified)
      .Where(t => t.BrokerId == brokerId)
      .Select(x => new TemplateBaseDTO
      {
        id = x.Id,
        Modified = x.Modified,
        templateText = x.templateText,
        TimesUsed = x.TimesUsed,
      })
      .ToListAsync();
    AllTemplatesDTO allTemplatesDTO = new AllTemplatesDTO
    {
      emailTemplates = Emailtemplates,
      smsTemplates = Smstemplates
    };
    return allTemplatesDTO;
  }
}
