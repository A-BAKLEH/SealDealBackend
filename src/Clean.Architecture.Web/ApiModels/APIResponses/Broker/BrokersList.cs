using Clean.Architecture.Core.DTOs.ProcessingDTOs;

namespace Clean.Architecture.Web.ApiModels.APIResponses.Broker;

public class BrokersList
{
  public List<BrokerForListDTO> brokers { get; set; }
}
