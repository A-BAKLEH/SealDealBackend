using Clean.Architecture.Web.AuthenticationAuthorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.LeadController;

public class LeadController : BaseApiController
{
  public LeadController(AuthorizeService authorizeService, IMediator mediator) : base(authorizeService, mediator)
  {
  }

}
