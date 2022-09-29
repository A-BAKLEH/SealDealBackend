using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.BrokerAggregate;

public class SmsTemplate : Entity<int>
{
  public string TemplateText { get; set; }

  public Broker Broker { get; set; }
  public Guid BrokerId { get; set; }
}

