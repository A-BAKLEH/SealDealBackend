using Clean.Architecture.Core.LeadAggregate;
using Clean.Architecture.SharedKernel;
namespace Clean.Architecture.Core.BrokerAggregate;

public class Tag : Entity<int>
{ 

  //public int? AgencyId { get; set; }
  //public Agency? Agency { get; set; }
  public string TagName { get; set; }

  public Broker Broker { get; set; }
  public Guid BrokerId { get; set; }

  public List<Lead> Leads { get; set; }
}
