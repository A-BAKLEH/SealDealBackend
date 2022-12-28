using Clean.Architecture.Core.Domain.TasksAggregate;
using Clean.Architecture.Core.ExternalServiceInterfaces.ProcessingInterfaces;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Web.Cache;
using Clean.Architecture.Web.Cache.Extensions;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Clean.Architecture.Web.MediatrRequests.EmailRequests;

public class EmailWebhookRequest : IRequest
{
  public bool isGmail { get; set; }
  public string email { get; set; }
}
public class EmailWebhookRequestHandler : IRequestHandler<EmailWebhookRequest>
{
  private AppDbContext _appDbContext;
  private IDistributedCache _distributedCache;
  public EmailWebhookRequestHandler(AppDbContext appDbContext, IDistributedCache distributedCache)
  {
    _appDbContext = appDbContext;
    _distributedCache = distributedCache;
  }

  public async Task<Unit> Handle(EmailWebhookRequest request, CancellationToken cancellationToken)
  {
    /*var Options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(12));
    BrokerCacheIndex brokerCacheIndex = null;
    FetchEmailsTask fetchEmailsTask1 = null;
    bool fetchEmailsTaskFromDb = false;
    //check/get/set broker index in cache
    if (!_distributedCache.TryGetValue<BrokerCacheIndex>(request.email, out brokerCacheIndex))
    {
      //get broker with Id,AccountActive, EmailsFetchTask
      var brokerWithFetchEmailsTask = _appDbContext.Brokers.
        Include(b => b.RecurrentTasks.Where(task => EF.Property<string>(task, "Discriminator") == nameof(FetchEmailsTask)))
        .Where(b => b.FirstConnectedEmail == request.email)
        .Select(b => new
        {
          b.Id,
          b.AccountActive,
          FetchEmailsTask = b.RecurrentTasks.First()
        }).FirstOrDefault();
      if (brokerWithFetchEmailsTask == null || !brokerWithFetchEmailsTask.AccountActive)
      {
        //TODO
        //No broker with that email/inactive, log error and unsubscribe from webhook for that gmail
        return Unit.Value;
      }
      //cache brokerIndex and FetchEmailsTask
      var fetchEmailsTaskId = brokerWithFetchEmailsTask.FetchEmailsTask.Id.ToString();
      brokerCacheIndex = new BrokerCacheIndex
      {
        BrokerId = brokerWithFetchEmailsTask.Id.ToString(),
        FetchEmailsTaskId = fetchEmailsTaskId
      };
      fetchEmailsTask1 = (FetchEmailsTask)brokerWithFetchEmailsTask.FetchEmailsTask;

      await _distributedCache.SetCacheAsync<BrokerCacheIndex>(request.email, brokerCacheIndex, Options);
    }
    //now index is not null

    //get FetchEmailsTask from Cache
    if (!_distributedCache.TryGetValue<FetchEmailsTask>(CacheKeyConstants.FetchEmailTaskPrefix + brokerCacheIndex.FetchEmailsTaskId,
        out fetchEmailsTask1))
    {
      //Index was cached but FetchEmailsTask is not
      if (fetchEmailsTask1 == null)
      {
        fetchEmailsTask1 = (FetchEmailsTask)_appDbContext.RecurrentTasks.Where(t => t.Id == int.Parse(brokerCacheIndex.FetchEmailsTaskId)).FirstOrDefault();
      }
      fetchEmailsTaskFromDb = true;
    }
    //task not processing or scheduled, schedule in 1 min
    if (fetchEmailsTask1.taskStatus != HangfireTaskStatus.Scheduled && fetchEmailsTask1.taskStatus != HangfireTaskStatus.Processing)
    {
      //schedule Hangfire Job in 1 min and update FetchEmailsTask in Cache and Db
      var HangfireTaskid = BackgroundJob.Schedule<IRecTaskProcessor>(I => I.RunEmailsFetchTask(fetchEmailsTask1.Id, request.isGmail), TimeSpan.FromMinutes(1));
      fetchEmailsTask1.taskStatus = HangfireTaskStatus.Scheduled;
      fetchEmailsTask1.ScheduledTime = DateTime.UtcNow;
      fetchEmailsTask1.HangfireTaskId = HangfireTaskid;
      await _distributedCache.SetCacheAsync<FetchEmailsTask>(request.email, fetchEmailsTask1, Options);
      if(fetchEmailsTaskFromDb) _appDbContext.Attach(fetchEmailsTask1);
      _appDbContext.SaveChanges();
    }
    else
    {
      if (fetchEmailsTaskFromDb) await _distributedCache.SetCacheAsync<FetchEmailsTask>(request.email, fetchEmailsTask1, Options);
    }*/
    return Unit.Value;
  }
}
