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
  /// rent or buy
  /// </summary>
  public string? leadType { get; set; }
  /// <summary>
  /// manual or automated
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
  /// coma-seprated list of integers for this agency's listings
  /// </summary>
  public List<int>? ListingsOfInterstIds { get; set; }

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
