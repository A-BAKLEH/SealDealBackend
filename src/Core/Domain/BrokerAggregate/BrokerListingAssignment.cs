using Core.Domain.AgencyAggregate;
using SharedKernel;

namespace Core.Domain.BrokerAggregate;
public class BrokerListingAssignment : EntityBase
{
  public Broker Broker { get; set; }
  public Guid BrokerId { get; set; }  
  public Listing Listing { get; set; }
  public int ListingId { get; set; }
  public DateTimeOffset assignmentDate { get; set; }

  /// <summary>
  /// who did the assignment
  /// </summary>
  public Guid? UserId { get; set; }
}
