using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.BrokerAggregate.EmailConnection;
public class ConnectedEmail : Entity<int>
{
  public Guid BrokerId { get; set; }

  /// <summary>
  /// in order of connection, 1 being the first
  /// </summary>
  public int EmailNumber { get; set; }
  public Broker Broker { get; set; }
  public string Email { get; set; }
  public bool isMSFT { get; set; }
  public EmailStatus EmailStatus { get; set; }
  public Guid GraphSubscriptionId { get; set; }
  public DateTimeOffset SubsExpiryDate { get; set; }
  public string SubsRenewalJobId { get; set; }

  /// <summary>
  /// when true, sync will happen shortly 
  /// </summary>
  public bool SyncScheduled { get; set; } = false;
  public string? SyncJobId { get; set; }
  /// <summary>
  /// Created property of last email fetched
  /// </summary>
  public DateTimeOffset LastSync { get; set; }

  /// <summary>
  /// DateTime of first email connection
  /// </summary>
  public DateTimeOffset FirstSync { get; set; }
  public List<FolderSync> FolderSyncs { get; set; }

}

//status after access to email confirmed and subscription webhook confirmed
public enum EmailStatus
{
  Good,
  Error,
  //waiting on first complete sync
  Waiting
}
