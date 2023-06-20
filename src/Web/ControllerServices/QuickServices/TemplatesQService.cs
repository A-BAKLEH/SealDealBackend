using Core.Constants.ProblemDetailsTitles;
using Core.Domain.BrokerAggregate.Templates;
using Core.Domain.LeadAggregate;
using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Exceptions;
using Web.ApiModels.APIResponses.Templates;
using Web.ApiModels.RequestDTOs;
using Web.HTTPClients;

namespace Web.ControllerServices.QuickServices;

public class TemplatesQService
{
    private readonly AppDbContext _appDbContext;
    private readonly OpenAIGPT35Service _openAi;

    public TemplatesQService(AppDbContext appDbContext, OpenAIGPT35Service openAIGPT35Service)
    {
        _appDbContext = appDbContext;
        _openAi = openAIGPT35Service;
    }
    public async Task<List<string>> DeleteTemplateAsync(int templateId, string tempType, Guid brokerId)
    {
        var actionPlans = await _appDbContext.ActionPlans.Where(ap => ap.BrokerId == brokerId)
          .Select(ap => new { ap.Name, actions = ap.Actions.Where(a => a.DataTemplateId == templateId) })
          .ToListAsync();
        var apNames = new List<string>();
        foreach (var item in actionPlans)
        {
            if (item.actions.Any()) apNames.Add(item.Name);
        }
        if (!apNames.Any())
        {
            await _appDbContext.Database.ExecuteSqlRawAsync($"DELETE FROM \"Templates\" WHERE \"Id\" = {templateId};");
        }
        return apNames;
    }

    public async Task<TemplateDTO> UpdateTemplateAsync(UpdateTemplateDTO dto, Guid brokerId)
    {
        var template = await _appDbContext.Templates.FirstAsync(t => t.Id == dto.TemplateId && t.BrokerId == brokerId);
        if (dto.text != null && dto.text != template.templateText)
        {
            template.templateText = dto.text;
        }
        if(dto.translatedText != null && dto.translatedText != template.translatedText)
        {
            template.translatedText = dto.translatedText;
        }
        if (dto.TemplateName != null) template.Title = dto.TemplateName;
        if (dto.TemplateType == "e" && template is EmailTemplate)
        {
            var temp = (EmailTemplate)template;
            if (dto.subject != null && dto.subject != temp.EmailTemplateSubject)
                temp.EmailTemplateSubject = dto.subject;
            if (dto.translatedSubject != null && dto.translatedSubject != temp.TranslatedEmailTemplateSubject)
                temp.TranslatedEmailTemplateSubject = dto.translatedSubject;
        }
        template.Modified = DateTime.UtcNow;
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
                Title = dto.TemplateName,
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
        var translated = await _openAi.TranslateTemplateAsync(dto.text);
        if(translated == null) throw new CustomBadRequestException("translation failed", ProblemDetailsTitles.TranslationFailed);
        var languageTranslation = Language.English;
        if(!Enum.TryParse<Language>(translated.translationlanguage, out languageTranslation))
        {
            if (translated.translationlanguage.ToLower().Contains("en")) languageTranslation = Language.English;
            else if (translated.translationlanguage.ToLower().Contains("fr")) languageTranslation = Language.French;
        };
        if(languageTranslation == Language.English) template.templateLanguage = Language.French;
        else template.templateLanguage = Language.English;
        template.translatedText = translated.translatedtext;
        if(template is EmailTemplate)
        {
            var temp = (EmailTemplate) template;
            var targetLang = temp.templateLanguage == Language.English ? "French" : "English";
            var translatedSubject = await _openAi.TranslateSubjectAsync(temp.EmailTemplateSubject, targetLang);
            if (string.IsNullOrEmpty(translatedSubject)) throw new CustomBadRequestException("email subject translation failed", "translation failed");
            temp.TranslatedEmailTemplateSubject = translatedSubject;
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
