namespace Core.DTOs.ProcessingDTOs;
public class LeadAppEventAllahLeadDTO
{
    public int id { get; set; }
    public DateTime EventTimeStamp { get; set; }
    public bool ReadByBroker { get; set; }
    public string EventType { get; set; }
    public Dictionary<string, string> NotifProps { get; set; }
    public bool IsActionPlanResult { get; set; }
    public string? BrokerComment { get; set; }

}
