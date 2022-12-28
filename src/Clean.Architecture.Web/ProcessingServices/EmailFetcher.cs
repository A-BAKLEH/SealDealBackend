using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Infrastructure.ExternalServices;

namespace Clean.Architecture.Web.ProcessingServices;

public class EmailFetcher
{
  private readonly AppDbContext _appDbContext;
  private readonly ILogger<EmailFetcher> _logger;
  private readonly ADGraphWrapper _aDGraph;
  public EmailFetcher(ILogger<EmailFetcher> logger,
    AppDbContext appDbContext,
    ADGraphWrapper aDGraph)
  {
    _appDbContext = appDbContext;
    _logger = logger;
    _aDGraph = aDGraph;
  }

  /// <summary>
  /// Check if email fetch for the email associated with this subsID is scheduled or not
   /// if not then Hangfire schedule (in 15 seconds) and update that info in ConnectedEmail
  /// </summary>
  /// <param name="SubsId"></param>
  /// <param name="TenantId"></param>
  /// <returns></returns>
  public async Task ScheduleFetch(Guid SubsId, string TenantId)
  {
    if (_appDbContext.ConnectedEmails.First(e => e.GraphSubscriptionId == SubsId).SyncScheduled) return;

  }
  public async Task FetchEmails(Guid SubsId, string TenantId)
  {
    _aDGraph.CreateClient(TenantId);

  }
}
