using System.Text.Json;
using Clean.Architecture.Core.Constants;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.ControllerServices.QuickServices;
using Clean.Architecture.Web.ProcessingServices;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;

namespace Clean.Architecture.Web.Api.EmailControllers;

public class MsftWebhook : BaseApiController
{
  private readonly ILogger<MsftWebhook> _logger;
  private readonly MSFTEmailQService _mSFTEmailQService;
  private readonly AppDbContext _appDbContext;
  private readonly EmailFetcher _emailFetcher;
  public MsftWebhook(AuthorizationService authorizeService,
    IMediator mediator,
    MSFTEmailQService mSFTEmailQService,
    ILogger<MsftWebhook> logger,
    AppDbContext dbContext,
    EmailFetcher emailFetcher) : base(authorizeService, mediator)
  {
    _logger = logger;
    _mSFTEmailQService = mSFTEmailQService;
    _appDbContext = dbContext;
    _emailFetcher = emailFetcher;
  }

  [HttpPost("Webhook")]
  public async Task<ActionResult<string>> WebhookProcess([FromQuery] string validationToken = null)
  {
    if(!string.IsNullOrEmpty(validationToken))
    {
      _logger.LogWarning("webhook validated");
      return Ok(validationToken);
    }

    var ProcessedSubsIDs = new List<Guid>();
    //process and return a 2xx response, or store and return 202 accepted if processing takes more than 3 sec
    //return 5xx so notif retried 
    using (StreamReader reader = new StreamReader(Request.Body))
    {
      string content = await reader.ReadToEndAsync();

      _logger.LogWarning("content of webhook POST body {content}",content);

      var notifications = JsonSerializer.Deserialize<ChangeNotificationCollection>(content);
      var HangfireSchedulingTasks = new List<Task>();

      if (notifications != null)
      {
        foreach (var notification in notifications.Value)
        {
          if (notification.ClientState != VariousCons.MSFtWebhookSecret)
          {
            _logger.LogCritical("Recevied MSFT notif with wrong Secret {WebhookSecret} with data {WebhookNotifData}",notification.ClientState,notification);
            return Ok("Dis Wallah");
          }

          var SubsId = (Guid)notification.SubscriptionId;
          if (ProcessedSubsIDs.Contains(SubsId)) continue;
          ProcessedSubsIDs.Add(SubsId);
          var subsId = (Guid) notification.SubscriptionId;
          var tenantId = notification.Resource.Split('/')[1].Split('@')[0];

          var FetchTask = _emailFetcher.ScheduleFetch(subsId, tenantId);
          HangfireSchedulingTasks.Add(FetchTask);      
        }
        Task.WaitAll(HangfireSchedulingTasks.ToArray());
      }
    }
    return Ok();
  }
}
