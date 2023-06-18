using Core.Domain.LeadAggregate;
using Core.DTOs.ProcessingDTOs;
using SharedKernel;

namespace Core.Domain.BrokerAggregate.Templates;
public abstract class Template : Entity<int>
{
    public Language templateLanguage {get;set;} = Language.English;
    public string templateText { get; set; }
    public string translatedText { get; set; }
    public Broker Broker { get; set; }
    public Guid BrokerId { get; set; }
    public DateTime Modified { get; set; }
    public int TimesUsed { get; set; } = 0;
    public int TimesSuccess { get; set; } = 0;
    public string Title { get; set; }
    public abstract TemplateDTO MapToDTO();
}
