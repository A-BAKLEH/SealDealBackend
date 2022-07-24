using Clean.Architecture.Web.AuthenticationAuthorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.LeadController;

public class LeadController : BaseApiController
{
  public LeadController(AuthorizeService authorizeService) : base(authorizeService)
  {
  }

}
