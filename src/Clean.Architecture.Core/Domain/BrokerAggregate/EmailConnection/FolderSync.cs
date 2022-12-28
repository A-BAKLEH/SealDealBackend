using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.BrokerAggregate.EmailConnection;
public class FolderSync : Entity<int>
{
  public int ConnectedEmailId { get; set; }
  public string FolderId { get; set; }
  public string FolderName { get; set; }
  public string DeltaToken { get; set; }
}
