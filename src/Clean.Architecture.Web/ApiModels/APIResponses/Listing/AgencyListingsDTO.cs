using Clean.Architecture.Core.DTOs.ProcessingDTOs;

namespace Clean.Architecture.Web.ApiModels.APIResponses.Listing;

public class AgencyListingsDTO
{
  public List<AgencyListingDTO> listings { get; set; }
}
