namespace Web.ApiModels.RequestDTOs.Admin;

public class ControlDTO
{
    public string key { get; set; }
    public bool ProcessEmails { get; set; } = true;
    public bool ProcessFailedEmailsParsing { get; set; } = true;
    public bool LogOpenAIEmailParsingObjects { get; set; } = false;
    public bool LogAllEmailsLengthsOpenAi { get; set; } = false;
}
