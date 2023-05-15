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
    /// only relevant for brokers, when admins assign them a listing to display the listings in red 
    /// on frontend
    /// </summary>
    public bool isSeen { get; set; } = false;

    /// <summary>
    /// who did the assignment
    /// </summary>
    public Guid? UserId { get; set; }
}
