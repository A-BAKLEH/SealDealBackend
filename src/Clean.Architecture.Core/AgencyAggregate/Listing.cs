
using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.Core.LeadAggregate;
using Clean.Architecture.SharedKernel;
namespace Clean.Architecture.Core.AgencyAggregate;

public enum ListingStatus
{
  Listed, Sold
}
public class Listing : Entity<int>
{
  public int AgencyId { get; set; }
  public Agency Agency { get; set; }
  public string Address { get; set; }
  public DateTime DateOfListing { get; set; }
  public ListingStatus Status { get; set; }
  public int Price { get; set; }
  public Guid? BrokerId { get; set; }
  public Broker AssignedBroker { get; set; }

  public List<Lead> InterestedLeads { get; set; }

}

