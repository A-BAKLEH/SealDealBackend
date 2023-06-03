using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;

namespace Core.DTOs.ProcessingDTOs;

public class EmailparserDBRecrodsRes
{
    public Lead Lead { get; set; }
    public List<AppEvent> appEvents { get; set; }
    public List<EmailEvent> emailEvents { get; set; }
    public bool LeadEmailUnsure { get; set; }
}
