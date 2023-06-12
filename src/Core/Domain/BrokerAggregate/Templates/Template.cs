using Core.DTOs.ProcessingDTOs;
using SharedKernel;

namespace Core.Domain.BrokerAggregate.Templates;
public abstract class Template : Entity<int>
{
  public string templateText { get; set; }
  public Broker Broker { get; set; }
  public Guid BrokerId { get; set; }
  public DateTime Modified { get; set; }
  public int? TimesUsed { get; set; }
  public string Title { get; set; }

  public abstract TemplateDTO MapToDTO();
}
