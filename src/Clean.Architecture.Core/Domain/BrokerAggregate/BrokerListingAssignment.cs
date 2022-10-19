using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.BrokerAggregate;
public class BrokerListingAssignment : EntityBase
{
  public Broker Broker { get; set; }
  public Guid BrokerId { get; set; }  
  public Listing Listing { get; set; }
  public int ListingId { get; set; }
  public DateTime assignmentDate { get; set; }
  //maybe add who assigned it
}
