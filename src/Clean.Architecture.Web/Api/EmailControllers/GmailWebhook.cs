using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.TasksAggregate;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Web.Cache;
using Clean.Architecture.Web.Cache.Extensions;
using Clean.Architecture.Web.ControllerServices;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Distributed;

namespace Clean.Architecture.Web.Api.EmailControllers;

public class GmailWebhook : BaseApiController
{
  private readonly ILogger<GmailWebhook> _logger;
  private readonly IDistributedCache _distributedCache;
  private AppDbContext _appDbContext;

  public const string EmailFetchTaskKey = "EmailFetchTask";
  public GmailWebhook(AuthorizationService authorizeService, IMediator mediator, AppDbContext appDbContext, ILogger<GmailWebhook> logger, IDistributedCache distributedCache) : base(authorizeService, mediator)
  {
    _logger = logger;
    _distributedCache = distributedCache;
    _appDbContext = appDbContext;
  }

  [HttpPost("Gmail-webhook")]
  public async Task<IActionResult> GmailWebhookExecute()
  {
    
    //would be sent by gmail push notification
    //this is a broker's connected email
    var email = "someone@gmail.com";

    // if brokerIndexCached == null retrieve BrokerId, AccountActive, FetchEmailsTask
    // if brokerIndexCached.FetchEmail == null
    //try BrokerIndex by connected Email
    BrokerCacheIndex brokerCacheIndex = null;
    FetchEmailsTask fetchEmailsTask1 = null;
    if (!_distributedCache.TryGetValue<BrokerCacheIndex>(email, out brokerCacheIndex))
    {
      //get broker with Id,AccountActive, EmailsFetchTask
      var brokerWithFetchEmailsTask = _appDbContext.Brokers.
        Include(b => b.RecurrentTasks.Where(task => EF.Property<string>(task, "Discriminator") == nameof(FetchEmailsTask)))
        .Where(b => b.FirstConnectedEmail == email)
        .Select(b => new
        {
          b.Id,
          b.AccountActive,
          FetchEmailsTask = b.RecurrentTasks.First()
        }).FirstOrDefault();
      if (brokerWithFetchEmailsTask == null)
      {
        //No broker with that email, log error and unsubscribe from webhook for that gmail
        return Ok();
      }
      if (!brokerWithFetchEmailsTask.AccountActive) return Ok(); //log to unsubscribe
      //cache brokerIndex and FetchEmailsTask
      var fetchEmailsTaskId = brokerWithFetchEmailsTask.FetchEmailsTask.Id.ToString();
      brokerCacheIndex = new BrokerCacheIndex
      {
        BrokerId = brokerWithFetchEmailsTask.Id.ToString(),
        FetchEmailsTaskId = fetchEmailsTaskId
      };
      fetchEmailsTask1 = (FetchEmailsTask) brokerWithFetchEmailsTask.FetchEmailsTask;
      var Options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(12));
      await _distributedCache.SetCacheAsync<BrokerCacheIndex>(email,brokerCacheIndex,Options);

      //get FetchEmailsTask
      if (!_distributedCache.TryGetValue<FetchEmailsTask>(CacheKeyConstants.FetchEmailTaskPrefix + fetchEmailsTask1.Id,
        out var whatever))
      {
        await _distributedCache.SetCacheAsync<FetchEmailsTask>(email, fetchEmailsTask1, Options);
      }
    }

    //task processing or scheduled, return OK()
    if (fetchEmailsTask1.taskStatus != HangfireTaskStatus.Scheduled && fetchEmailsTask1.taskStatus != HangfireTaskStatus.Processing)
    {
      //schedule Hangfire Job and update FetchEmailsTask in Cache
      //var id = BackgroundJob.Schedule<Interface>(RunEmailFetchTask(TaskId)) in 1 min
    }
    return Ok();

   }
    //-> use Hisotry.List(token) API to get a list of {emailId, type of notif (added, deleted, whatever)}
}
