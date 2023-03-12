using System.Text.Json;
using Core.Constants;
using Infrastructure.Data;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;
using Web.Processing.EmailAutomation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;

namespace Web.Api.EmailControllers;

public class MsftWebhook : BaseApiController
{
  private readonly ILogger<MsftWebhook> _logger;
  private readonly EmailProcessor _emailProcessor;
  public MsftWebhook(AuthorizationService authorizeService,
    IMediator mediator,
    ILogger<MsftWebhook> logger,
    EmailProcessor emailProcessor) : base(authorizeService, mediator)
  {
    _logger = logger;
    _emailProcessor = emailProcessor;
  }

  [HttpPost("Webhook")]
  public async Task<ActionResult<string>> WebhookProcess([FromQuery] string validationToken = null)
  {
    _logger.LogWarning("webhook frapped");
    if (!string.IsNullOrEmpty(validationToken))
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
          var tenantId = notification.Resource.Split('/')[1].Split('@')[0];

          var syncTask = _emailProcessor.CheckEmailSyncAsync(SubsId, tenantId);
          HangfireSchedulingTasks.Add(syncTask);      
        }
        Task.WaitAll(HangfireSchedulingTasks.ToArray());
      }
    }
    return Ok();
  }
}
