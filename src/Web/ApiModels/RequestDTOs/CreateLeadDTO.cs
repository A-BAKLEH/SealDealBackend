using System.ComponentModel.DataAnnotations;

namespace Web.ApiModels.RequestDTOs;

/// <summary>
/// for manual lead creation by admin or broker
/// </summary>
public class CreateLeadDTO
{
  public bool AssignToSelf { get; set; }
  /// <summary>
  /// ONLY if admin assigns lead to a broker that is not himself.
  /// </summary>
  public Guid? AssignToBrokerId { get; set; }
  public string? AssignToBrokerFullName { get; set;}

  [Required(AllowEmptyStrings = false)]
  public string LeadFirstName { get; set; }

  [Required(AllowEmptyStrings = false)]
  public string LeadLastName { get; set; }
  [Phone]
  public string? PhoneNumber { get; set; }
  [EmailAddress]
  public string? Email { get; set; }
  public int? Budget { get; set; }

  /// <summary>
  /// adds a note to the lead.
  /// </summary>
  public string? leadNote { get; set; }
  /// <summary>
  /// renter or buyer or Unknown
  /// </summary>
  public string? leadType { get; set; }

  /// <summary>
  /// area string
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
  public string? TagToAdd { get; set; }
}
