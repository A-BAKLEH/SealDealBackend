namespace Clean.Architecture.Web.ApiModels.RequestDTOs;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class NewBrokerDTO
{
  [Required(AllowEmptyStrings = false)]
  public string FirstName { get; set; }

  [Required(AllowEmptyStrings =false)]
  public string LastName { get; set; }

  [Phone]
  public string? PhoneNumber { get; set; }

  [EmailAddress]
  public string Email { get; set; }

  [ValidateNever]
  public string failureReason { get; set; } = "";
}
