using Core.DTOs.ProcessingDTOs;

namespace Core.Domain.BrokerAggregate.Templates;

public class SmsTemplate : Template
{
  public override TemplateDTO MapToDTO()
  {
    Console.WriteLine("executing from EmailTemplate");
    var dto = new TemplateDTO
    {
      id = Id,
      Modified = Modified.UtcDateTime,
      templateText = templateText,
      TimesUsed = TimesUsed,
      Title = Title,
      type = "s"
    };
    return dto;
  }
}

