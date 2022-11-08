using System.ComponentModel.DataAnnotations;

namespace Clean.Architecture.Web.ApiModels.RequestDTOs;

public class CustomerPortalRequestDTO
{
  [Required]
  [Url]
  public string ReturnUrl { get; set; }
}
