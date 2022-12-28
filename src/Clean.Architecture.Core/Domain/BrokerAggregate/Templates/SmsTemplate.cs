using Clean.Architecture.Core.DTOs.ProcessingDTOs;

namespace Clean.Architecture.Core.Domain.BrokerAggregate.Templates;

public class SmsTemplate : Template
{
  public override TemplateDTO MapToDTO()
  {
    Console.WriteLine("executing from EmailTemplate");
    var dto = new TemplateDTO
    {
      id = Id,
      Modified = Modified,
      templateText = templateText,
      TimesUsed = TimesUsed,
      Title = Title,
      type = "s"
    };
    return dto;
  }
}

