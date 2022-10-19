
using Clean.Architecture.Core.Domain.ActionPlanAggregate;
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.NotificationAggregate;
using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.LeadAggregate;

public enum LeadStatus
{
  New, Active, Client, Closed, Dead
}

public class Lead : Entity<int>
{
  public int AgencyId { get; set; }
  public Agency Agency { get; set; }
  public string LeadFirstName { get; set; }
  public string? LeadLastName { get; set; }
  public string? PhoneNumber { get; set; }
  public string? Email { get; set; }
  public int? Budget { get; set; }
  public DateTime EntryDate { get; set; } = DateTime.UtcNow;
  public LeadSource source { get; set; }
  public LeadType leadType { get; set; }
  /// <summary>
  /// name of website for example
  /// </summary>
  public string? leadSourceDetails { get; set; }
  public LeadStatus LeadStatus { get; set; } = LeadStatus.New;
  /// <summary>
  /// just a string for now, dont use areasOfInterest
  /// </summary>
  public string? Areas { get;set; }
  public Broker? Broker { get; set; }
  public Guid? BrokerId { get; set; }
  public List<Area>? AreasOfInterest { get; set; }
  /// <summary>
  /// lisitng that brought the lead
  /// </summary>
  public int? ListingId { get; set; }
  public Listing? Listing { get; set; }
  public Note? Note { get; set; }
  public List<Tag>? Tags { get; set; }
  public List<ActionPlanAssociation>? ActionPlanAssociations { get; set; }
  public List<Notification>? LeadHistoryEvents { get; set; }
}
public enum LeadSource
{
  manualBroker, emailAuto,SmsAuto, adminAssign, unknown
}

public enum LeadType
{
  /// <summary>
  /// someone who wants to find a property to Buy
  /// </summary>
  Buyer,
  /// <summary>
  /// someone who wants to find a property to rent
  /// </summary>
  Renter,
  Unknown
}

