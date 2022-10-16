using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.BrokerAggregate;

public class EmailTemplate : Entity<int>
{
  public string EmailTemplateSubject { get; set; }
  public string EmailTemplateText { get; set; }
  public Guid BrokerId { get; set; }
  public Broker Broker { get; set; }

}

