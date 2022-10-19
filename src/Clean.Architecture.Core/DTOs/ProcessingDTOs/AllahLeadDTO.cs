using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.Core.Domain.NotificationAggregate;

namespace Clean.Architecture.Core.DTOs.ProcessingDTOs;
public class AllahLeadDTO
{
  public string LeadFirstName { get; set; }
  public string? LeadLastName { get; set; }
  public string? PhoneNumber { get; set; }
  public string? Email { get; set; }
  public int? Budget { get; set; }
  public DateTime EntryDate { get; set; }
  public LeadSource leadSource { get; set;  }
  public LeadType leadType { get; set; }
  public string? leadSourceDetails { get; set;}
  public LeadStatus LeadStatus { get; set; }
  //public IEnumerable<AreaDTO>? AreasOfInterest { get; set; }
  //public IEnumerable<LeadListingDTO>? OriginalListing { get; set; }
  public string? Areas { get; set; }
  public Note? Note { get; set; }
  public IEnumerable<TagDTO>? Tags { get; set; }
  public IEnumerable<ActionPlanAssociationDTO>? ActionPlanAssociations { get; set; }
  public List<NotifExpandedDTO>? LeadHistoryEvents { get; set; }
  /// <summary>
  /// id of the 
  /// </summary>
  public int lastLeadEventID { get; set; }
}
