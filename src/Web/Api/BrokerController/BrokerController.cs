using Core.Config.Constants.LoggingConstants;
using Core.Constants.ProblemDetailsTitles;
using Core.Domain.BrokerAggregate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Exceptions.CustomProblemDetails;
using Web.ApiModels.RequestDTOs;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;
using Web.ControllerServices.StaticMethods;
using Web.MediatrRequests.BrokerRequests;

namespace Web.Api.BrokerController;
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
            _logger.LogCritical("{tag} inactive or non-admin mofo User with UserId {userId}", TagConstants.Unauthorized, id);
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
        if (failedBrokers != null && failedBrokers.Any())
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
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive mofo User with UserId {userId}", TagConstants.Unauthorized, id);
            return Unauthorized();
        }
        var brokers = await _brokerTagsQService.GetBrokersByAdmin(brokerTuple.Item1.AgencyId);

        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
        foreach (var broker in brokers)
        {
            broker.created = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, broker.created);
        }
        if (brokers == null || !brokers.Any()) return NotFound();
        return Ok(brokers);
    }

    [HttpDelete("{BrokerId}")]
    public async Task<IActionResult> DeleteBroker(Guid BrokerId)
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item3 || !brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} non-admin or inactive mofo User with UserId {userId}", TagConstants.Unauthorized, id);
            return Unauthorized();
        }
        await _brokerTagsQService.DeleteBrokerAsync(BrokerId, id, brokerTuple.Item1.AgencyId);
        return Ok();
    }
}
