namespace Web.ApiModels;

public class UpdateLeadDTO
{
    public bool? VerifyEmailAddress { get; set; }
    public string? LeadFirstName { get; set; }
    public string? LeadLastName { get; set; }
    public string? PhoneNumber { get; set; }
    public List<string>? Emails { get; set; }
    public int? Budget { get; set; }

    //options: Buyer Renter Unknown
    public string? LeadType { get; set; }

    //options : Hot, Active, Delayed, Cold,Closed, Dead
    public string? LeadStatus { get; set; }
    public string? Areas { get; set; }
    public string? leadNote { get; set; }
    public string? language { get; set; }
}
