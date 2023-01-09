using Core.DTOs.ProcessingDTOs;

namespace Web.ApiModels.APIResponses.Broker;

public class BrokersList
{
  public List<BrokerForListDTO> brokers { get; set; }
}
