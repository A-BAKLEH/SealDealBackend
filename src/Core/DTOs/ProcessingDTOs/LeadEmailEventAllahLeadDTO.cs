namespace Core.DTOs.ProcessingDTOs;

public class LeadEmailEventAllahLeadDTO
{
    public string EmailId { get; set; }
    public string BrokerEmail { get; set; }
    public bool Seen { get; set; } = false;
    /// <summary>
    /// or forwarded
    /// </summary>
    public bool RepliedTo { get; set; } = false;
    /// <summary>
    /// needs reply or forward
    /// </summary>
    public bool NeedsAction { get; set; } = false;
}
