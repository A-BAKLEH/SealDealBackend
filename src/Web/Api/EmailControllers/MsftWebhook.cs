﻿using Core.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Web.ApiModels;
using Web.ControllerServices;
using Web.Processing.EmailAutomation;

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
        if (!string.IsNullOrEmpty(validationToken))
        {
            return Ok(validationToken);
        }
        //return Ok();
        var ProcessedSubsIDs = new List<Guid>();
        //process and return a 2xx response, or store and return 202 accepted if processing takes more than 3 sec
        //return 5xx so notif retried 
        using (StreamReader reader = new StreamReader(Request.Body))
        {
            string content = await reader.ReadToEndAsync();

            _logger.LogWarning("content of webhook POST body {content}", content);

            //var notifications = JsonSerializer.Deserialize<ChangeNotificationCollection>(content);
            var notifications = JsonSerializer.Deserialize<ChangeNotifWebhookDTO>(content);
            var HangfireSchedulingTasks = new List<Task>();

            if (notifications != null)
            {
                foreach (var notification in notifications.value)
                {
                    if (notification.clientState != VariousCons.MSFtWebhookSecret)
                    {
                        _logger.LogCritical("Recevied MSFT notif with wrong Secret {WebhookSecret} with data {WebhookNotifData}", notification.clientState, notification);
                        return Ok("Dis Wallah");
                    }

                    var SubsId = Guid.Parse(notification.subscriptionId);
                    if (ProcessedSubsIDs.Contains(SubsId)) continue;
                    ProcessedSubsIDs.Add(SubsId);
                    //var tenantId = notification.Resource.Split('/')[1].Split('@')[0];
                    var tenantId = notification.tenantId;
                    var syncTask = _emailProcessor.CheckEmailSyncAsync(SubsId, tenantId);
                    HangfireSchedulingTasks.Add(syncTask);
                }
                Task.WaitAll(HangfireSchedulingTasks.ToArray());
            }
        }
        return Ok();
    }
}
