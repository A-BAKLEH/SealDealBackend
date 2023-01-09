namespace Core.DTOs.ProcessingDTOs;
public class NotifExpandedDTO
{
  public int id { get; set; }
  public DateTimeOffset UnderlyingEventTimeStamp { get; set; }
  public string NotifType { get; set; }
  public bool ReadByBroker { get; set; }
  public bool NotifyBroker{ get; set;}
  public Dictionary<string, string> NotifProps { get; set; }
  public string? NotifData { get; set; }
  public string? BrokerComment { get; set; }

}
