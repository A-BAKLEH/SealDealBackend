
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
using Clean.Architecture.SharedKernel.Exceptions.CustomProblemDetails;
using Clean.Architecture.Core.Constants.ProblemDetailsTitles;
using Humanizer;
using TimeZoneConverter;
using System;

namespace Clean.Architecture.Web.Api.BrokerController;
[Authorize]
public class BrokerController : BaseApiController
{
  private readonly ILogger<BrokerController> _logger;
  private readonly BrokerQService _brokerTagsQService;
  public BrokerController(AuthorizationService authorizeService, IMediator mediator, BrokerQService brokerTagsQService, ILogger<BrokerController> logger) : base(authorizeService, mediator)
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

  [HttpPost]
  public async Task<IActionResult> AddBrokers([FromBody] IEnumerable<NewBrokerDTO> brokers)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
    if (!brokerTuple.Item3 || !brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive or non-admin mofo User with UserId {UserId} tried to add brokers", TagConstants.Unauthorized, id);
      return Forbid();
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
    if (nonValidBrokers.Count > 0)
    {
      var res1 = new BadRequestProblemDetails
      {
        Title = ProblemDetailsTitles.InvalidInput,
        Detail = "Initial validation for some brokers failed, no brokers added",
        Status = 400,
        Errors = nonValidBrokers
      };
      return BadRequest(res1);
    }
    var failedBrokers = await _mediator.Send(command);
    //some or all brokers failed adding to B2C
    //TODO later add specific problems if possible, such as duplicate B2C email
    if(failedBrokers != null && failedBrokers.Any())
    {
      var res2 = new BadRequestProblemDetails
      {
        Title = ProblemDetailsTitles.B2CAccountAddFailure,
        Detail = "B2C adding failed for following brokers",
        Status = 400,
        Errors = failedBrokers
      };
      return BadRequest(res2);
    }
    return Ok();
  }

  /// <summary>
  /// used by admin only
  /// </summary>
  /// <returns></returns>
  [HttpGet("All")]
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
    var timeZoneInfo = TZConvert.GetTimeZoneInfo(brokerTuple.Item1.IanaTimeZone);
    foreach (var broker in brokers)
    {
      broker.created = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, broker.created);
    }
    if (brokers == null || !brokers.Any()) return NotFound();
    var res = new BrokersList { brokers = brokers };
    return Ok(res);
  }
}
