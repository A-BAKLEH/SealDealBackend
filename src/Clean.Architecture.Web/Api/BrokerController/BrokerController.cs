
using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.MediatrRequests.BrokerRequests;
using Clean.Architecture.Web.ApiModels;
using Clean.Architecture.Web.ControllerServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    //var agencyObject = _agencyRepository.GetById(adminObj.AgencyId);
    var command = new AddBrokersRequest();
    List<NewBrokerDTO> nonValidBrokers = new();
    foreach (var broker in brokers)
    {
      if (BrokerDTOValid(broker)) command.brokers.Add(new Broker
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

  static bool BrokerDTOValid(NewBrokerDTO brokerDTO)
  {
    bool valid = true;
    if (string.IsNullOrWhiteSpace(brokerDTO.FirstName)) { brokerDTO.failureReason += "first name invalid;"; valid = false; }
    if (string.IsNullOrWhiteSpace(brokerDTO.LastName)) { brokerDTO.failureReason += "first name invalid;"; valid = false; }
    if (!IsValidEmail(brokerDTO.Email)) { brokerDTO.failureReason += "email invalid;"; valid = false; }
    return valid;
  }

  static bool IsValidEmail(string email)
  {
    try
    {
      var addr = new System.Net.Mail.MailAddress(email);
      return addr.Address == email && email.Contains('.');
    }
    catch
    {
      return false;
    }
  }
}
