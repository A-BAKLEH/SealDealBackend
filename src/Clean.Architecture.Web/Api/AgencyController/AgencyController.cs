using Microsoft.AspNetCore.Mvc;
using Clean.Architecture.Web.ControllerServices;
using MediatR;
using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.Web.ApiModels.APIResponses.Listing;
using Clean.Architecture.Web.ControllerServices.QuickServices;
using Clean.Architecture.Web.ApiModels.RequestDTOs;

namespace Clean.Architecture.Web.Api.Agencycontroller;

public class AgencyController : BaseApiController
{

  private readonly ILogger<AgencyController> _logger;
  private readonly AgencyQService _agencyQService;
  public AgencyController( AuthorizationService authorizeService,AgencyQService agencyQService, IMediator mediator, ILogger<AgencyController> logger ) : base(authorizeService, mediator)
  {
    _logger = logger;
    _agencyQService = agencyQService;
  }

  /// <summary>
  /// will later implement paging
  /// </summary>
  /// <param name="includeSold">1 means true</param>
  /// <param name="lastListingId"></param>
  /// <returns></returns>
  [HttpGet("AllListings/{includeSold}/{lastListingId}")]
  public async Task<IActionResult> GetAgencyListings(int includeSold, int lastListingId)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item3)
    {
      _logger.LogWarning("[{Tag}] inactive mofo User with UserId {UserId} tried to get Listings", TagConstants.Inactive, id);
      return Unauthorized();
    }
    var listings = await _agencyQService.GetAgencyListings(brokerTuple.Item1.AgencyId, includeSold == 1 ? true : false);

    if (listings == null || !listings.Any()) return NotFound();
    var respnse = new BrokersListingsDTO { listings = listings };
    return Ok(respnse);
  }

  [HttpPost("CreateListing")]
  public async Task<IActionResult> CreateAgencyListing([FromBody] CreateListingRequestDTO dto)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item3 || !brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive or non-admin mofo User with UserId {UserId} tried to get Listings", TagConstants.Inactive, id);
      return Unauthorized();
    }

    var listing = await _agencyQService.CreateListing(brokerTuple.Item1.AgencyId, dto);
    return Ok(listing);
  }
}
