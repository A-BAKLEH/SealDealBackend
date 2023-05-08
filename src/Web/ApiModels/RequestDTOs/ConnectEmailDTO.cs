using System.ComponentModel.DataAnnotations;


namespace Web.ApiModels.RequestDTOs;
public class ConnectEmailDTO
{
    //m for msft, g for google
    [Required(AllowEmptyStrings = false)]
    public string EmailProvider { get; set; }

    [EmailAddress]
    public string Email { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string TenantId { get; set; }

    /// <summary>
    /// only relevant for admins, if false then leads won't be automatically assigned to brokers, they will
    /// show up in the unassigned leads list in admin view
    /// </summary>
    public bool AssignLeadsAuto { get; set; } = false;

}
