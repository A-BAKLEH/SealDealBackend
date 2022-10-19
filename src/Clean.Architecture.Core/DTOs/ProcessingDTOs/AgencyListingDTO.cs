namespace Clean.Architecture.Core.DTOs.ProcessingDTOs;
public class AgencyListingDTO
{
  public string Address { get; set; }
  public DateTime DateOfListing { get; set; }
  public string Status { get; set; }
  public int Price { get; set; }
  public string? ListingURL { get; set; }
  public int GeneratedLeadsCount { get; set; }
  public IEnumerable<BrokerPerListingDTO>? AssignedBrokers { get; set; }
}
