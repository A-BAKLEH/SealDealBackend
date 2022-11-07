using Clean.Architecture.Core.DTOs.ProcessingDTOs;

namespace Clean.Architecture.Core.Domain.BrokerAggregate;

public class SmsTemplate : Template
{
  public override TemplateDTO MapToDTO()
  {
    Console.WriteLine("executing from EmailTemplate");
    var dto = new TemplateDTO
    {
      id = this.Id,
      Modified = this.Modified,
      templateText = this.templateText,
      TimesUsed = this.TimesUsed,
      Title = this.Title,
      type = "s"
    };
    return dto;
  }
}

