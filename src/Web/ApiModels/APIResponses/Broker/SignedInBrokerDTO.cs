using Core.DTOs;

namespace Web.ApiModels.APIResponses.Broker;

public class SignedInBrokerDTO
{
    public AccountStatusDTO AccountStatus { get; set; } = new();

    public int AgencyId { get; set; }
    public Guid BrokerId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public Boolean isAdmin { get; set; }
    public string? PhoneNumber { get; set; }
    public string LoginEmail { get; set; }
    public bool markEmailsRead { get; set; }
    public DateTime Created { get; set; }
    public bool SoloBroker { get; set;}
    public string BrokerLanguage { get; set; }
    public bool hasConnectedCalendar { get; set; }
    public bool CalendarSyncEnabled { get; set; }
}
