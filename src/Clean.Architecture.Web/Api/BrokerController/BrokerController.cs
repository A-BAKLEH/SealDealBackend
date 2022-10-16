
using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.MediatrRequests.BrokerRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Clean.Architecture.Web.ControllerServices.StaticMethods;
using Clean.Architecture.Web.ApiModels.RequestDTOs;
using Clean.Architecture.Web.ControllerServices.QuickServices;
using Clean.Architecture.Web.ApiModels.APIResponses.Broker;

namespace Clean.Architecture.Web.Api.BrokerController;
[Authorize]
public class BrokerController : BaseApiController
{
  private readonly ILogger<BrokerController> _logger;
  private readonly BrokerTagsQService _brokerTagsQService;
  public BrokerController(AuthorizationService authorizeService, IMediator mediator, BrokerTagsQService brokerTagsQService, ILogger<BrokerController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
    _brokerTagsQService = brokerTagsQService;
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

  [HttpPost("Create-Tag/{tagname}")]
  public async Task<IActionResult> CreateTag(string tagname)
  {
    //Not checking active, permissions
    var brokerId = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var result = await _mediator.Send(new CreateBrokerTagRequest { BrokerId = brokerId, TagName = tagname });
    if (result.Success) return Ok();
    return BadRequest(result);
  }

  [HttpGet("Get-Tags")]
  public async Task<IActionResult> GetTags()
  {
    //Not checking active, permissions
    var brokerId = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);

    var tags = await _brokerTagsQService.GetBrokerTagsAsync(brokerId);
    if (tags == null) return NotFound();
    return Ok(tags);
  }

  /// <summary>
  /// used by admin only
  /// </summary>
  /// <returns></returns>
  [HttpGet("Get-Brokers-List")]
  public async Task<IActionResult> GetBrokers()
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item3 || !brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] non-admin mofo User with UserId {UserId} or Non-paying tried to add brokers", TagConstants.Unauthorized, id);
      return Unauthorized();
    }
    var brokers = await _brokerTagsQService.GetBrokersByAdmin(brokerTuple.Item1.AgencyId);
    if (brokers == null || !brokers.Any()) return NotFound();
    var res = new BrokersList { brokers = brokers };
    return Ok(res);
  }


}
