using Clean.Architecture.Core.DTOs.ProcessingDTOs;

namespace Clean.Architecture.Web.ApiModels.APIResponses;

public class NotifsResponseDTO
{ 
  public List<WrapperNotifDashboard> data { get; set; }
}
