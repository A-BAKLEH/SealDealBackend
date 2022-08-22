
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate.Specifications;
using Clean.Architecture.SharedKernel.Repositories;
using Clean.Architecture.Web.ApiModels;
using Clean.Architecture.Web.ControllerServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Clean.Architecture.Web.Api.BillingController;
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BillingController : BaseApiController
{

  private readonly IRepository<Broker> _repository;
  public BillingController(IRepository<Broker> repository, AuthorizationService authorizeService, IMediator mediator) : base(authorizeService, mediator)
  {
    _repository = repository;
  }

  [HttpPost("customer-portal")]
  public async Task<IActionResult> CustomerPortal([FromBody] CustomerPortalRequestDTO req)
  {
    var l = User.Claims.ToList();
    Guid b2cBrokerId = Guid.Parse(l.Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);


    //var authAgency = _agencyRepository.GetById(authAdmin.AgencyId);
    var authAdmin = await _repository.GetBySpecAsync(new BrokerByIdWithAgencySpec(b2cBrokerId));
    var authAgency = authAdmin.Agency;

    var options = new Stripe.BillingPortal.SessionCreateOptions
    {
      Customer = authAgency.AdminStripeId,
      ReturnUrl = req.ReturnUrl,
    };

    var service = new Stripe.BillingPortal.SessionService();
    var session = await service.CreateAsync(options);

    return Ok(new
    {
      url = session.Url
    });


    return Ok();
  }
}
