using Infrastructure.Data;
using Web.Processing.EmailAutomation;

namespace Web.Processing.ActionPlans;

public class APProcessor
{
  private readonly AppDbContext _appDbContext;
  private readonly ILogger<APProcessor> _logger;
  public APProcessor(AppDbContext appDbContext, ILogger<APProcessor> logger)
  {
    _appDbContext = appDbContext;
    _logger = logger;
  }

  public async Task DoActionAsync(int ActionId)
  {
    _logger.LogInformation($"doiing action with id {ActionId}");
    return;
  }
}
