using Clean.Architecture.Web.ControllerServices;
using MediatR;

namespace Clean.Architecture.Web.Api.EmailControllers;

public class MsftWebhook : BaseApiController
{
  private readonly ILogger<MsftWebhook> _logger;
  public MsftWebhook(AuthorizationService authorizeService, IMediator mediator, ILogger<MsftWebhook> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
  }
}
