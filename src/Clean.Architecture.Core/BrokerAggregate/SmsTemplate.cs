using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.BrokerAggregate;

public class SmsTemplate : Entity<int>
{
  public string TemplateText { get; set; }

  //public int? AgencyId { get; set; }
  //public Agency? Agency { get; set; }

  public Broker Broker { get; set; }
  public Guid BrokerId { get; set; }
}

