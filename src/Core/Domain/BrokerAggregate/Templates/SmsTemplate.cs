using Core.DTOs.ProcessingDTOs;

namespace Core.Domain.BrokerAggregate.Templates;

public class SmsTemplate : Template
{
    public override TemplateDTO MapToDTO()
    {
        var dto = new TemplateDTO
        {
            id = Id,
            Modified = Modified,
            templateText = templateText,
            TimesUsed = TimesUsed,
            Title = Title,
            type = "s",
            Language = templateLanguage.ToString(),
            translatedText = translatedText,
        };
        return dto;
    }
}

