using Clean.Architecture.Core.AgencyAggregate;
using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.Core.BrokerAggregate.Specifications;
using Clean.Architecture.SharedKernel.Interfaces;
using Clean.Architecture.Web.ApiModels;
using Clean.Architecture.Web.AuthenticationAuthorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Clean.Architecture.Web.Api.BillingController;
[Route("api/[controller]")]
[ApiController]
public class BillingController : BaseApiController
{

  private readonly IRepository<Broker> _repository;
  public BillingController(IRepository<Broker> repository, AuthorizationService authorizeService, IMediator mediator) : base(authorizeService, mediator)
  {
    _repository = repository;
  }


  [Authorize]
  [HttpPost("customer-portal")]
  public async Task<IActionResult> CustomerPortal([FromBody] CustomerPortalRequestDTO req)
  {
    StripeConfiguration.ApiKey = "sk_test_51LHCXSIAg7HKu3" +
               "TPU6Ess0RMvvdMbFiZw0GwWfgDqZkFUFXtYwTY5XRbjqyJrAnJ8arSQ12k3heATZSbsK6GJyEI00txFG34FH";

    Guid b2cBrokerId;

    try
    {
      var isAuth = User.Identity.IsAuthenticated;
      if (!isAuth)
      {
        throw new Exception("not auth");
      }
      var l = User.Claims.ToList();

      b2cBrokerId = Guid.Parse(l.Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);


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
    }
    catch (StripeException e)
    {
      Console.WriteLine(e.StripeError.Message);
      return BadRequest(e.StripeError.Message);
    }

    return Ok();
  }
}
