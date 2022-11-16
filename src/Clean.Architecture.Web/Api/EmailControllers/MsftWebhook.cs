using Clean.Architecture.Web.ControllerServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;

namespace Clean.Architecture.Web.Api.EmailControllers;

public class MsftWebhook : BaseApiController
{
  private readonly ILogger<MsftWebhook> _logger;
  public MsftWebhook(AuthorizationService authorizeService, IMediator mediator, ILogger<MsftWebhook> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
  }

  /// <summary>
  /// connects user's email to notifications
  /// </summary>
  /// <returns></returns>
  [Authorize]
  [HttpPost("connect")]
  public async Task<IActionResult> ConnectEmail()
  {
    //get clientState secret from environment variable
    // set user connected email in db, appply obo flow to store token in cache
    //subscribe to graph api tokens

    /*POST https://graph.microsoft.com/v1.0/subscriptions
    Content - Type: application / json
    {
          "changeType": "created,updated",
      "notificationUrl": "https://webhook.azurewebsites.net/notificationClient",
      "resource": "/me/mailfolders('inbox')/messages",
      "expirationDateTime": "2016-03-20T11:00:00.0000000Z",
      "clientState": "SecretClientState"
    }*/
    return Ok();
  }

  [HttpPost("notifs/graph")]
  public async Task<IActionResult> WebhookProcess()
  {

    return Ok();
  }
}
