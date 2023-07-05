using Core.Config.Constants.LoggingConstants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Exceptions;
using System.Security.Claims;
using Web.ApiModels.APIResponses;
using Web.ApiModels.RequestDTOs;
using Web.ControllerServices;
using Web.MediatrRequests.StripeRequests;

namespace Web.Api.BillingController;

[Authorize]
public class BillingController : BaseApiController
{
    private readonly ILogger<BillingController> _logger;
    public BillingController(AuthorizationService authorizeService, IMediator mediator, ILogger<BillingController> logger) : base(authorizeService, mediator)
    {
        _logger = logger;
    }

    [HttpPost("Portal")]
    public async Task<IActionResult> CreateBillingPortal([FromBody] CustomerPortalRequestDTO req)
    {
        var brokerTuple = await this._authorizeService.AuthorizeUser(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)), true);
        if (!brokerTuple.Item3)
        {
            _logger.LogCritical("{tag} nonAdmin mofo User", TagConstants.Unauthorized);
            return Forbid();
        }
        Guid b2cBrokerId = brokerTuple.Item1.Id;

        var portalURL = await _mediator.Send(
          new CreateBillingPortalRequest
          {
              AgencyStripeId = brokerTuple.Item1.Agency.AdminStripeId,
              returnURL = req.ReturnUrl
          });
        return Ok(new BillingPortalResponse
        {
            portalURL = portalURL
        });
    }

    [HttpPost("CheckoutSession")]
    public async Task<IActionResult> CreateChekoutSession([FromBody] CheckoutSessionRequestDTO req)
    {
        Guid b2cBrokerId;
        int AgencyID;

        var brokerTuple = await this._authorizeService.AuthorizeUser(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)), true);
        if (!brokerTuple.Item3)
        {
            _logger.LogCritical("{tag} non-admin mofo User", TagConstants.Unauthorized);
            return Forbid();
        }
        b2cBrokerId = brokerTuple.Item1.Id;
        AgencyID = brokerTuple.Item1.AgencyId;

        _logger.LogInformation("{tag} Creating a CheckoutSession for User in" +
          " Agency with AgencyId {agencyId} with PriceID {priceID} and Quantity {quantity}", TagConstants.CheckoutSession, AgencyID, req.PriceId, req.Quantity);

        var checkoutSessionDTO = await _mediator.Send(new CreateCheckoutSessionRequest
        {
            agency = brokerTuple.Item1.Agency,
            priceID = req.PriceId,
            Quantity = req.Quantity >= 1 ? req.Quantity : 1,
        });

        if (string.IsNullOrEmpty(checkoutSessionDTO.sessionId)) throw new InconsistentStateException("CreateCheckoutSession-nullOrEmpty SessionID", $"session ID is {checkoutSessionDTO.sessionId}", b2cBrokerId.ToString());
        _logger.LogInformation("{tag} Created a CheckoutSession with ID {checkoutSessionId} for User in" +
          " Agency with AgencyId {AgencyId}", TagConstants.CheckoutSession, checkoutSessionDTO.sessionId, AgencyID);
        return Ok(checkoutSessionDTO);
    }
}
