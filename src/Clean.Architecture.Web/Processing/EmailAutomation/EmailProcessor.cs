using Clean.Architecture.Infrastructure.Data;
namespace Clean.Architecture.Web.Processing.EmailAutomation;

public class EmailProcessor
{
  private readonly AppDbContext _appDbContext;
  private readonly ILogger<EmailProcessor> _logger;
  public EmailProcessor(AppDbContext appDbContext, ILogger<EmailProcessor> logger)
  {
    _appDbContext = appDbContext;
    _logger = logger;
  }
  /// <summary>
  /// fetch all emails from last sync date and process them
  /// 
  /// </summary>
  /// <param name="connEmailId"></param>
  /// <param name="tenantId"></param>
  public void SyncEmail(int connEmailId, string tenantId)
  {

  }
}
