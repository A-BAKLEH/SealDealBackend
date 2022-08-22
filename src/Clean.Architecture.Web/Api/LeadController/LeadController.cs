using Clean.Architecture.Web.ControllerServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Clean.Architecture.Web.Api.LeadController;

[Authorize]
public class LeadController : BaseApiController
{
  public LeadController(AuthorizationService authorizeService, IMediator mediator) : base(authorizeService, mediator)
  {
  }

}
