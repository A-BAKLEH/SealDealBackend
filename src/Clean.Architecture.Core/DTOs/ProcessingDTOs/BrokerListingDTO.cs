using Clean.Architecture.Core.Domain.AgencyAggregate;

namespace Clean.Architecture.Core.DTOs.ProcessingDTOs;
public class BrokerListingDTO
{
  public Address Address { get; set; }
  public DateTime DateOfListing { get; set; }
  public string Status { get; set; }
  public int Price { get; set; }
  public string? ListingURL { get; set; }
  public DateTime DateAssignedToMe { get; set; }
  public int AssignedBrokersCount { get; set; }
}
