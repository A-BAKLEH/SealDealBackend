namespace Web.Cache;

/// <summary>
/// helper class to encapsulate Broker's Properties Ids
/// </summary>
public class BrokerCacheIndex
{
  /// <summary>
  /// guid BrokerId converted to string
  /// </summary>
  public string BrokerId { get; set; }
  /// <summary>
  /// without prefix
  /// </summary>
  public string? FetchEmailsTaskId { get; set; }

}
