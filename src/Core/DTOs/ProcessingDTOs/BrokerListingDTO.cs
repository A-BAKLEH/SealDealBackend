using Core.Domain.AgencyAggregate;

namespace Core.DTOs.ProcessingDTOs;
public class BrokerListingDTO
{
  public int ListingId { get; set; }
  public Address Address { get; set; }
  public DateTime DateOfListing { get; set; }
  public string Status { get; set; }
  public int Price { get; set; }
  public string? ListingURL { get; set; }
  public DateTime DateAssignedToMe { get; set; }
  public int AssignedBrokersCount { get; set; }
}
