namespace Clean.Architecture.Core.Domain.AgencyAggregate;
public class Address
{
  public string BuildingNumber { get; set; }
  public string Street { get; set; }
  public string? AppartmentNo { get; set; }
  public string City { get; set; }
  public string ProvinceState { get; set; }
  public string Country { get; set; }
  public string PostalCode { get; set; }

}
