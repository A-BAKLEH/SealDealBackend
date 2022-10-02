
using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Web.ApiModels;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.MediatrRequests.BrokerRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Clean.Architecture.Web.ControllerServices.StaticMethods;

namespace Clean.Architecture.Web.Api.BrokerController;
[Authorize]
public class BrokerController : BaseApiController
{
  private readonly ILogger<BrokerController> _logger;
  public BrokerController(AuthorizationService authorizeService, IMediator mediator, ILogger<BrokerController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
  }

  /*[HttpGet("get-subscription-quantities")]
  public async Task<IActionResult> GetCurrentSubscriptionQuantities()
  {
    return Ok();
    /*var auth = User.Identity.IsAuthenticated;
    if (!auth) throw new Exception("not auth");

    var brokerTuple = this._authorizeService.AuthorizeUser(Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value), true);
    if (brokerTuple.Item2 == false || brokerTuple.Item3 == false) return Unauthorized();


    return Ok(new SubsQuantityDTO
    {
      StripeSubsQuantity = brokerTuple.Item1.Agency.NumberOfBrokersInSubscription,
      BrokersQuantity = brokerTuple.Item1.Agency.NumberOfBrokersInDatabase
    });*/

  [HttpPost("add-brokers")]
  public async Task<IActionResult> AddBrokers([FromBody] IEnumerable<NewBrokerDTO> brokers)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
    if (!brokerTuple.Item3 || !brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] non-admin mofo User with UserId {UserId} or Non-paying tried to add brokers", TagConstants.Unauthorized, id);
      return Unauthorized();
    }
    var command = new AddBrokersRequest();
    command.admin = brokerTuple.Item1;
    List<NewBrokerDTO> nonValidBrokers = new();
    foreach (var broker in brokers)
    {
      if (BrokerHelperMethods.BrokerDTOValid(broker)) command.brokers.Add(new Broker
      {
        FirstName = broker.FirstName,
        LastName = broker.LastName,
       LoginEmail = broker.Email,
        PhoneNumber = broker.PhoneNumber
      });
      else nonValidBrokers.Add(broker);
    }
    if (nonValidBrokers.Count > 0) return BadRequest(nonValidBrokers);
    var failedBrokers = await _mediator.Send(command);
    return Ok(failedBrokers);
  }

  
}
