using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.BrokerAggregate.EmailConnection;
public class ConnectedEmail : Entity<int>
{
  public Guid BrokerId { get; set; }

  /// <summary>
  /// in order of connection, or according to importance
  /// </summary>
  public int EmailNumber { get; set; }
  public Broker Broker { get; set; }
  public string Email { get; set; }
  public bool isMSFT { get; set; }
  public EmailStatus EmailStatus { get; set; }

  public Guid GraphSubscriptionId { get; set; }
  public DateTime SubsExpiryDate { get; set; }
  public string SubsRenewalJobId { get; set; }

  public bool SyncScheduled { get; set; } = false;
  public DateTime LastSync { get; set; }
  public DateTime FirstSync { get; set; }
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
