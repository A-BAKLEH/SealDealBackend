namespace Core.Domain.AgencyAggregate;
public class Address
{
    /// <summary>
    /// 'building number' 'street name
    /// </summary>
    public string StreetAddress { get; set; }
    public string apt { get; set; }
    public string City { get; set; }
    public string ProvinceState { get; set; }
    public string Country { get; set; }
    public string PostalCode { get; set; }
}
