using Clean.Architecture.Core.DTOs.ProcessingDTOs;

namespace Clean.Architecture.Core.Domain.BrokerAggregate.Templates;

public class EmailTemplate : Template
{
  public string EmailTemplateSubject { get; set; }

  public override TemplateDTO MapToDTO()
  {
    Console.WriteLine("executing from EmailTemplate");
    var dto = new TemplateDTO
    {
      id = Id,
      Modified = Modified,
      subject = EmailTemplateSubject,
      templateText = templateText,
      TimesUsed = TimesUsed,
      Title = Title,
      type = "e"
    };
    return dto;
  }
}

