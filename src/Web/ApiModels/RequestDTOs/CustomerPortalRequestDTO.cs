using System.ComponentModel.DataAnnotations;

namespace Web.ApiModels.RequestDTOs;

public class CustomerPortalRequestDTO
{
  [Required]
  [Url]
  public string ReturnUrl { get; set; }
}
