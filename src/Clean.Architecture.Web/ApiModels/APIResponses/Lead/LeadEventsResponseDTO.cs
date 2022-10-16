using Clean.Architecture.Core.DTOs.ProcessingDTOs;

namespace Clean.Architecture.Web.ApiModels.APIResponses.Lead;

public class LeadEventsResponseDTO
{
  public List<NotifExpandedDTO> events { get; set; }
}
