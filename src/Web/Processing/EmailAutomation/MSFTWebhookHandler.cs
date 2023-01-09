using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Processing.EmailAutomation;

public class MSFTWebhookHandler
{
  private readonly AppDbContext _appDbContext;
  private readonly ILogger<MSFTWebhookHandler> _logger;
  public MSFTWebhookHandler(AppDbContext appDbContext, ILogger<MSFTWebhookHandler> logger)
  {
    _logger = logger;
    _appDbContext = appDbContext;
  }

  /// <summary>
  /// checks if the email with SubsId has a scheduled sync, if yes dont do anything if no then schedules
  /// it in 15 secs
  /// </summary>
  /// <param name="SubsId"></param>
  /// <param name="tenantId"></param>
  /// <returns></returns>
  public async Task CheckEmailSyncAsync(Guid SubsId, string tenantId)
  {
    //ADDCACHE
    var connEmail = await  _appDbContext.ConnectedEmails.FirstAsync(e => e.GraphSubscriptionId == SubsId);
    if(!connEmail.SyncScheduled)
    {
      var jobId = Hangfire.BackgroundJob.Schedule<EmailProcessor>(e => e.SyncEmail(connEmail.Id,tenantId), TimeSpan.FromSeconds(15));
      connEmail.SyncScheduled = true;
      connEmail.SyncJobId= jobId;
    }
  }
}
