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
  public async Task<List<string>> DeleteTemplateAsync(int templateId, string tempType, Guid brokerId)
  {
    var actionPlans = await _appDbContext.ActionPlans.Where(ap => ap.BrokerId == brokerId)
      .Select(ap => new { ap.Name, actions = ap.Actions.Where(a => a.DataId == templateId) })
      .ToListAsync();
    var apNames = new List<string>();
    foreach (var item in actionPlans)
    {
      if (item.actions.Any()) apNames.Add(item.Name);
    }
    if (!apNames.Any())
    {
      Template temp;
      if (tempType == "e") temp = new EmailTemplate { Id = templateId };
      else temp = new SmsTemplate { Id = templateId };

      _appDbContext.Templates.Remove(temp);
      await _appDbContext.SaveChangesAsync();
    }
    return apNames;
  }


    public async Task<TemplateDTO> UpdateTemplateAsync(UpdateTemplateDTO dto, Guid brokerId)
    {
      dynamic template = await _appDbContext.Templates.FirstAsync(t => t.Id == dto.TemplateId && t.BrokerId == brokerId);

      if (dto.text != null) template.templateText = dto.text;
      if (dto.TemplateName != null) template.Title = dto.TemplateName;
      if (dto.TemplateType == "e" && dto.subject != null) template.EmailTemplateSubject = dto.subject;
      template.Modified = DateTimeOffset.UtcNow;
      await _appDbContext.SaveChangesAsync();
      return template.MapToDTO();
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
