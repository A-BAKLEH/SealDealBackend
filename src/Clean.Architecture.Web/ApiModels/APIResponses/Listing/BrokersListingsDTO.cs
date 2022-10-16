using Clean.Architecture.Core.DTOs.ProcessingDTOs;

namespace Clean.Architecture.Web.ApiModels.APIResponses.Listing;

public class BrokersListingsDTO
{
  public List<ListingDTO> listings { get; set; }
}
