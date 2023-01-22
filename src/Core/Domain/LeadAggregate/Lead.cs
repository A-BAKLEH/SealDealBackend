
using Core.Domain.ActionPlanAggregate;
using Core.Domain.AgencyAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate.Interactions;
using Core.Domain.NotificationAggregate;
using SharedKernel;

namespace Core.Domain.LeadAggregate;

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
  public DateTimeOffset EntryDate { get; set; }
  public LeadSource source { get; set; }
  public LeadType leadType { get; set; }
  /// <summary>
  /// siteName, adminCreaterId (when assigne to broker by admin), emailId ()
  /// </summary>
  public Dictionary<string, string> SourceDetails { get; set; } = new();
  public LeadStatus LeadStatus { get; set; } = LeadStatus.New;
  /// <summary>
  /// true if created by admin manually OR automated, then assigned to a broker, then Id of admin is in source details
  /// null if lead created and assigned to admin by himself or if just created
  /// false if lead created by broker and assigned to himself
  /// </summary>
  //public bool? AssignedByAdmin { get; set; }
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
  public List<LeadInteraction>? LeadInteractions { get; set; }
}
public enum LeadSource
{
  manual, emailAuto,SmsAuto,unknown
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

