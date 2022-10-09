using System.ComponentModel.DataAnnotations;

namespace Clean.Architecture.Web.ApiModels.RequestDTOs;

public class CustomerPortalRequestDTO
{
  [Required]
  public string ReturnUrl { get; set; }
}
