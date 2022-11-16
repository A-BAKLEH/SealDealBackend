namespace Clean.Architecture.Web.ApiModels.RequestDTOs;

public class AddressCreateDTO
{
  /// <summary>
  /// 'building number' 'street name' 'appartment'
  /// </summary>
  public string StreetAddress { get; set; }
  public string City { get; set; }
  public string ProvinceState { get; set; }
  public string Country { get; set; }
  public string PostalCode { get; set; }
}
