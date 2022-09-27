using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.TasksAggregate;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Web.Cache;
using Clean.Architecture.Web.Cache.Extensions;
using Clean.Architecture.Web.ControllerServices;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Clean.Architecture.Web.Api.EmailControllers;

public class GmailWebhook : BaseApiController
{
  private readonly ILogger<GmailWebhook> _logger;
  private readonly IDistributedCache _distributedCache;
  private AppDbContext _appDbContext;

  public const string EmailFetchTaskKey = "EmailFetchTask";
  public GmailWebhook(AuthorizationService authorizeService, IMediator mediator,AppDbContext appDbContext, ILogger<GmailWebhook> logger, IDistributedCache distributedCache) : base(authorizeService, mediator)
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
    

    //try broker Id by connected Email
    var brokerIdCached = await _distributedCache.GetAsync<BrokerIdByConnEmailCache>(email);
    if(brokerIdCached != null)
    {
      var FetchEmailsTaskCached = await _distributedCache.GetAsync<FetchEmailsTask>(brokerIdCached.Id);
      if(FetchEmailsTaskCached != null)
      {
        if(FetchEmailsTaskCached.taskStatus != HangfireTaskStatus.Scheduled)
        {
            //schedule HAngfire Job and update FetchEmailsTask
            _appDbContext.Attach(FetchEmailsTaskCached);
            await _appDbContext.SaveChangesAsync();    
        }
      }
    }
    //-> use Hisotry.List(token) API to get a list of {emailId, type of notif (added, deleted, whatever)}
    return Ok();
  }
  /*
   * 
   * cache will have key = BrokerId for brokers only
   * for other entities, it will have key {prefix-id}
   * and for separated Data it will have other kinds of keys like connectedEmail for BrokerIdByConnEmailCache type
   * 
   * Exceptions: FetchEmailsTask entity has key BrokerId
   */
}
