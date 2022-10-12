namespace Clean.Architecture.Web.ApiModels.RequestDTOs;

public class CreateLeadDTO
{
  public string? LeadFirstName { get; set; }
  public string? LeadLastName { get; set; }
  public string? PhoneNumber { get; set; }
  public string? Email { get; set; }
  public int? Budget { get; set; }
  public string? leadNote { get; set; }

}
