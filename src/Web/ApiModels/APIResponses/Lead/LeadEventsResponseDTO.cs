using Core.DTOs.ProcessingDTOs;

namespace Web.ApiModels.APIResponses.Lead;

public class LeadEventsResponseDTO
{
  public List<NotifExpandedDTO> events { get; set; }
}
