using Core.DTOs.ProcessingDTOs;

namespace Core.Domain.BrokerAggregate.Templates;

public class EmailTemplate : Template
{
    public string EmailTemplateSubject { get; set; }
    public string TranslatedEmailTemplateSubject { get; set; }
    public override TemplateDTO MapToDTO()
    {
        var dto = new TemplateDTO
        {
            id = Id,
            Modified = Modified,
            subject = EmailTemplateSubject,
            templateText = templateText,
            TimesUsed = TimesUsed,
            Title = Title,
            type = "e",
            Language = templateLanguage.ToString(),
            translatedSubject = TranslatedEmailTemplateSubject,
            translatedText = translatedText,
        };
        return dto;
    }
}

