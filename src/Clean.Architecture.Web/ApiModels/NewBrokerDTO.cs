namespace Clean.Architecture.Web.ApiModels;

public class NewBrokerDTO
{
  public int frontendId { get; set; }
  public string FirstName { get; set; }

  public string LastName { get; set; }

  public string? PhoneNumber { get; set; }

  public string Email { get; set; }

  public string failureReason { get; set; } = "";
}
