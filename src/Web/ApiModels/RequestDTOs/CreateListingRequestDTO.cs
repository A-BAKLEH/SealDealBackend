namespace Web.ApiModels.RequestDTOs;
using System.ComponentModel.DataAnnotations;
public class CreateListingRequestDTO
{
  public DateTime DateOfListing { get; set; }
  public int Price { get; set; }
  public List<Guid>? AssignedBrokersIds { get; set; }
  public string? URL { get; set; }
  public AddressCreateDTO Address { get; set; }
  /// <summary>
  /// listed , sold
  /// </summary>
  [Required(AllowEmptyStrings = false)]
  public string Status { get; set; }
}
