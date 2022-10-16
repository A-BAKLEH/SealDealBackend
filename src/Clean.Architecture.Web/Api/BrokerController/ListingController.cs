using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.Web.ApiModels.APIResponses.BadRequests;
using Clean.Architecture.Web.ApiModels.APIResponses.Listing;
using Clean.Architecture.Web.ApiModels.RequestDTOs;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.ControllerServices.QuickServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.BrokerController;
[Authorize]
public class ListingController : BaseApiController
{
  private readonly ILogger<ListingController> _logger;
  private readonly BrokerTagsQService _brokerTagsQService; 
  private readonly AgencyQService _agencyQService;
  public ListingController(AuthorizationService authorizeService, IMediator mediator,
    BrokerTagsQService brokerTagsQService,
    AgencyQService agencyQService,
    ILogger<ListingController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
    _brokerTagsQService = brokerTagsQService;
    _agencyQService = agencyQService;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <returns>listings assigned to broker only. If admin calls it, will return listings that are assigned
  /// explicitly to him, not all agency's listings
  /// associatedto other brokers</returns>
  [HttpGet("MyListings/{includeSold}")]
  public async Task<IActionResult> GetBrokerListings(int includeSold)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive mofo User with UserId {UserId} tried to get Listings", TagConstants.Inactive, id);
      return Unauthorized();
    }
    var listings = await _brokerTagsQService.GetBrokersListings(id,includeSold == 1 ? true : false);
    
    if (listings == null || !listings.Any()) return NotFound();
    var respnse = new BrokersListingsDTO { listings = listings };
    return Ok(respnse);
  }


  [HttpPost("AssignToBroker/{listingid}/{brokerid}")]
  public async Task<IActionResult> AssignListingToBroker(int listingid, Guid brokerId)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item3 || !brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive or non-admin mofo User with UserId {UserId} tried to get Listings", TagConstants.Inactive, id);
      return Unauthorized();
    }

    var response = await _agencyQService.AssignListingToBroker(listingid, brokerId);
    if (response != null)
    {
      var details = new BadRequestDetails { message = response };
      return BadRequest(details);
    }
    return Ok();
  }
  [HttpDelete("DetachFromBroker/{listingid}/{brokerid}")]
  public async Task<IActionResult> DetachListingFromBroker(int listingid, Guid brokerId)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item3 || !brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive or non-admin mofo User with UserId {UserId} tried to get Listings", TagConstants.Inactive, id);
      return Unauthorized();
    }

    var response = await _agencyQService.DetachBrokerFromListing(listingid, brokerId);
    if (response != null)
    {
      var details = new BadRequestDetails { message = response };
      return BadRequest(details);
    }
    return Ok();
  }
}
