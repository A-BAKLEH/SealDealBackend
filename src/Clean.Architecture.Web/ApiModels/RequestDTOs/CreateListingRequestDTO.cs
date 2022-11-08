namespace Clean.Architecture.Web.ApiModels.RequestDTOs;
using System.ComponentModel.DataAnnotations;
public class CreateListingRequestDTO
{
  public DateTime DateOfListing { get; set; }
  public int Price { get; set; }
  public List<Guid>? AssignedBrokersIds { get; set; }
  public string? URL { get; set; }

  [Required(AllowEmptyStrings = false)]
  public string Address { get; set; }
  /// <summary>
  /// listed l , sold s
  /// </summary>
  [Required(AllowEmptyStrings = false)]
  public string Status { get; set; }
}
