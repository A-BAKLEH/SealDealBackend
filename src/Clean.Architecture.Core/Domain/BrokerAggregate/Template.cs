using Clean.Architecture.Core.DTOs.ProcessingDTOs;
using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.BrokerAggregate;
public abstract class Template : Entity<int>
{
  public string templateText{ get; set; }
  public Broker Broker { get; set; }
  public Guid BrokerId { get; set; }
  public DateTime Modified { get; set; }
  public int? TimesUsed { get; set; }
  public string Title { get; set; }

  public abstract TemplateDTO MapToDTO();
}
