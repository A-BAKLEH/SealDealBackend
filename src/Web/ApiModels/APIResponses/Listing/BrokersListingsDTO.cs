using Core.DTOs.ProcessingDTOs;

namespace Web.ApiModels.APIResponses.Listing;

public class BrokersListingsDTO
{
  public List<BrokerListingDTO> listings { get; set; }
}
