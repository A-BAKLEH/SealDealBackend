namespace Core.DTOs.ProcessingDTOs;
public class LeadForListDTO
{
    public int LeadId { get; set; }
    public string LeadFirstName { get; set; }
    public string? LeadLastName { get; set; }
    public string language { get; set; }
    public string? PhoneNumber { get; set; }
    public List<LeadEmailDTO> Emails { get; set; }
    public int? Budget { get; set; }
    public DateTime EntryDate { get; set; }
    public string source { get; set; }
    public string leadType { get; set; }
    public Dictionary<string, string> leadSourceDetails { get; set; }
    public string LeadStatus { get; set; }
    public NoteDTO? Note { get; set; }
    public IEnumerable<TagDTO>? Tags { get; set; }

}

public class LeadEmailDTO
{
    public string email { get; set; }
    public bool isMain { get; set; } = false;
}
public class NoteDTO
{
    public int id { get; set; }
    public string NoteText { get; set; }
}

