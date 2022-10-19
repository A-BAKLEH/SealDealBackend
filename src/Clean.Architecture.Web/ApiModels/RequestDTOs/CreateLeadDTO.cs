namespace Clean.Architecture.Web.ApiModels.RequestDTOs;

public class CreateLeadDTO
{
  public string? LeadFirstName { get; set; }
  public string? LeadLastName { get; set; }
  public string? PhoneNumber { get; set; }
  public string? Email { get; set; }
  public int? Budget { get; set; }
  public string? leadNote { get; set; }
  /// <summary>
  /// renter or buyer or Unknown
  /// </summary>
  public string? leadType { get; set; }
  /// <summary>
  /// manualBroker, emailAuto,SmsAuto, adminAssign, unknown
  /// </summary>
  public string? leadSource { get; set; }
  /// <summary>
  /// name of website if automated
  /// </summary>
  public string? leadSourceDetails { get; set; }

  /// <summary>
  /// 
  /// </summary>
  public string? Areas { get; set; }
  /// <summary>
  /// ID of the listing which brought the lead
  /// </summary>
  public int? ListingOfInterstId { get; set; }

  /// <summary>
  /// existing tags to assign to this lead
  /// </summary>
  public List<int>? TagsIds { get; set; }
  /// <summary>
  /// new tag to add and assign to this lead
  /// 
  /// </summary>
  public string TagToAdd { get; set; }
}
