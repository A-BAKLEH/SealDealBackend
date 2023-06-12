using Core.Domain.LeadAggregate;

namespace Core.DTOs.ProcessingDTOs;
public class AllahLeadDTO
{
    public int LeadId { get; set; }
    public bool verifyEmailAddress { get; set; }
    public Guid? brokerId { get; set; }
    public string LeadFirstName { get; set; }
    public string? LeadLastName { get; set; }
    public string language { get; set; }
    public string? PhoneNumber { get; set; }
    public List<LeadEmailDTO> Emails { get; set; }
    public int? Budget { get; set; }
    public DateTime EntryDate { get; set; } // local
    public DateTime LastNotifsViewedAt { get; set; } // local
    public LeadSource leadSource { get; set; }
    public string leadType { get; set; }
    public Dictionary<string, string> leadSourceDetails { get; set; }
    public string LeadStatus { get; set; }
    public string? Areas { get; set; }
    public Note? Note { get; set; }
    public IEnumerable<TagDTO>? Tags { get; set; }
    public IEnumerable<ActionPlanAssociationDTO>? ActionPlanAssociations { get; set; }
    public IEnumerable<LeadAppEventAllahLeadDTO>? LeadAppEvents { get; set; }
    public IEnumerable<LeadEmailEventAllahLeadDTO> leadEmailEvents{ get; set; }
    /// <summary>
    /// id of the 
    /// </summary>
    public int lastLeadEventID { get; set; }
}
