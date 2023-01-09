namespace Core.DTOs.ProcessingDTOs;
public class LeadForListDTO
{
  public int LeadId { get; set; }
  public string LeadFirstName { get; set; }
  public string? LeadLastName { get; set; }
  public string? PhoneNumber { get; set; }
  public string? Email { get; set; }
  public int? Budget { get; set; }
  public DateTime EntryDate { get; set; }
  public string source { get; set; }
  public string leadType { get; set; }
  public string? leadSourceDetails { get; set; }
  public string LeadStatus { get; set; }
  public NoteDTO? Note { get; set; }
  public IEnumerable<TagDTO>? Tags { get; set; }

}

public class NoteDTO
{
  public int id { get; set; }
  public string NoteText { get; set; }
}

