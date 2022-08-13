using Clean.Architecture.Core.Domain.AgencyAggregate;

namespace Clean.Architecture.Core.Domain.LeadAggregate;

public enum Event
{

  sms, email, phone, changeOfStatus
}

public class History
{
  public int HistoryId { get; set; }
  public int? AgencyId { get; set; }
  public Agency? Agency { get; set; }
  public int LeadId { get; set; }
  public Lead Lead { get; set; }
  public Event Event { get; set; }
  public DateTime EventTimestamp { get; set; }
  public string EventSubject { get; set; }
  public string EventDescription { get; set; }

}
