namespace Clean.Architecture.Core.DTOs.ProcessingDTOs;
public class ListingDTO
{
  public string Address { get; set; }
  public DateTime DateOfListing { get; set; }
  public string Status { get; set; }
  public int Price { get; set; }
  public string? ListingURL { get; set; }
  public int InterestedLeadsCount { get; set; }

  public string? BrokerName { get; set; }
  public string? BrokerLName { get; set; }
  public Guid? AssignedBrokerId { get; set; }
}
