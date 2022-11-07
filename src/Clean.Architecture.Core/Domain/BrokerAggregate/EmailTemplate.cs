using Clean.Architecture.Core.DTOs.ProcessingDTOs;

namespace Clean.Architecture.Core.Domain.BrokerAggregate;

public class EmailTemplate : Template
{
  public string EmailTemplateSubject { get; set; }

  public override TemplateDTO MapToDTO()
  {
    Console.WriteLine("executing from EmailTemplate");
    var dto = new TemplateDTO
    {
      id = this.Id,
      Modified = this.Modified,
      subject = this.EmailTemplateSubject,
      templateText = this.templateText,
      TimesUsed = this.TimesUsed,
      Title = this.Title,
      type = "e"
    };
    return dto;
  }
}

