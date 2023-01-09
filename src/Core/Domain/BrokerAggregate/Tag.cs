
using Core.Domain.LeadAggregate;
using SharedKernel;
namespace Core.Domain.BrokerAggregate;

public class Tag : Entity<int>
{ 
  public string TagName { get; set; }
  public Broker Broker { get; set; }
  public Guid BrokerId { get; set; }
  public List<Lead> Leads { get; set; }
}
