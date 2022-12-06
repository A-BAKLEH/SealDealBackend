using System.ComponentModel.DataAnnotations;

namespace Clean.Architecture.Web.ApiModels.RequestDTOs;

public class AdminConsentDTO
{
  [EmailAddress]
  public string Email { get; set; }

  [Required(AllowEmptyStrings = false)]
  public string TenantId { get; set; }
}
