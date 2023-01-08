
using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.SharedKernel;
namespace Clean.Architecture.Core.Domain.BrokerAggregate;

public class Tag : Entity<int>
{ 
  public string TagName { get; set; }
  public Broker Broker { get; set; }
  public Guid BrokerId { get; set; }
  public List<Lead> Leads { get; set; }
}
