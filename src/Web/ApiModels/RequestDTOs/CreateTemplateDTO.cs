namespace Web.ApiModels.RequestDTOs;
using System.ComponentModel.DataAnnotations;
public class CreateTemplateDTO
{
    /// <summary>
    /// "e" for email, "s" for sms
    /// </summary>
    ///
    [Required(AllowEmptyStrings = false)]
    public string TemplateType { get; set; }
    /// <summary>
    /// subject for email
    /// </summary>
    public string? subject { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string text { get; set; }
    /// <summary>
    /// displayed name
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string TemplateName { get; set; }
}
