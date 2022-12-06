using System.ComponentModel.DataAnnotations;


namespace Clean.Architecture.Web.ApiModels.RequestDTOs;
public class ConnectEmailDTO
{
  //m for msft, g for google
  [Required(AllowEmptyStrings = false)]
  public string EmailProvider { get; set; }

  [EmailAddress]
  public string Email { get; set; }

  [Required(AllowEmptyStrings = false)]
  public string TenantId { get; set; }
}
