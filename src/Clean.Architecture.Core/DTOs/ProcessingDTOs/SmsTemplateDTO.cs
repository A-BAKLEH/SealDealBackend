namespace Clean.Architecture.Core.DTOs.ProcessingDTOs;
public class SmsTemplateDTO
{
  public int id { get; set;  }
  public string templateText { get; set; }
  public DateTime Modified { get; set; }
  public int? TimesUsed { get; set; }
  public string Title { get; set; }
}
