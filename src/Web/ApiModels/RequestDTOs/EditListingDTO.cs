namespace Web.ApiModels.RequestDTOs;

public class EditListingDTO
{
  public string? URL { get; set; }
  public int? Price { get; set; }

  /// <summary>
  /// Listed, Sold
  /// </summary>
  public string? Status { get; set; }

}
