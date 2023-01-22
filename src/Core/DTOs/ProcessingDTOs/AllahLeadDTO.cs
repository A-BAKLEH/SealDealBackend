using Core.Domain.LeadAggregate;

namespace Core.DTOs.ProcessingDTOs;
public class AllahLeadDTO
{
  public string LeadFirstName { get; set; }
  public string? LeadLastName { get; set; }
  public string? PhoneNumber { get; set; }
  public string? Email { get; set; }
  public int? Budget { get; set; }
  public DateTime EntryDate { get; set; }
  public LeadSource leadSource { get; set;  }
  public string leadType { get; set; }
  public Dictionary<string,string> leadSourceDetails { get; set;}
  public string LeadStatus { get; set; }
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
