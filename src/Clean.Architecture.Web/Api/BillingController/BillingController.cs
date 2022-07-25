using Clean.Architecture.Core.AgencyAggregate;
using Clean.Architecture.SharedKernel.Interfaces;
using Clean.Architecture.Web.ApiModels;
using Clean.Architecture.Web.AuthenticationAuthorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.BillingController;
[Route("api/[controller]")]
[ApiController]
public class BillingController : BaseApiController
{

  private readonly IRepository<Agency> _repository;
  public BillingController(IRepository<Agency> repository, AuthorizeService authorizeService) : base(authorizeService)
  {
    _repository = repository;
  }

  [Authorize]
  [HttpPost("customer-portal")]
  public async Task<IActionResult> CustomerPortal([FromBody] CustomerPortalRequestDTO req)
  {
    return Ok();
  }
}
