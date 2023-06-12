
using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using SharedKernel;
namespace Core.Domain.AgencyAggregate;

public enum ListingStatus
{
    Listed, Sold
}
public class Listing : Entity<int>
{
    public int AgencyId { get; set; }
    public Agency Agency { get; set; }
    public Address Address { get; set; }
    public string FormattedStreetAddress { get; set; }
    /// <summary>
    /// client timeZ
    /// </summary>
    public DateTime DateOfListing { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Listed;
    public int Price { get; set; }
    public byte AssignedBrokersCount { get; set; } = 0;
    public List<BrokerListingAssignment>? BrokersAssigned { get; set; }
    public string? URL { get; set; }
    /// <summary>
    /// count includes leads that are deleted now
    /// </summary>
    public int LeadsGeneratedCount { get; set; }
    public List<Lead>? LeadsGenerated { get; set; }

}

