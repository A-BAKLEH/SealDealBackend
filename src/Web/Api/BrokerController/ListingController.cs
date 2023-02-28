using Core.Config.Constants.LoggingConstants;
using Web.ApiModels.RequestDTOs;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;
using Web.ControllerServices.StaticMethods;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Web.Api.BrokerController;
[Authorize]
public class ListingController : BaseApiController
{
  private readonly ILogger<ListingController> _logger;
  private readonly BrokerQService _brokerTagsQService; 
  private readonly ListingQService _listingQService;
  public ListingController(AuthorizationService authorizeService, IMediator mediator,
    BrokerQService brokerTagsQService,
    AgencyQService agencyQService,
    ListingQService listingQService,
    ILogger<ListingController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
    _brokerTagsQService = brokerTagsQService;
    _listingQService = listingQService;
  }

  /// <summary>
  /// will later implement paging
  /// </summary>
  /// <param name="includeSold">1 means true</param>
  /// <param name="lastListingId"></param>
  /// <returns>
  /// </returns>
  [HttpGet("All/{includeSold}/{lastListingId}")]
  public async Task<IActionResult> GetAgencyListings(int includeSold, int lastListingId)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item3 || !brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive or non-admin mofo User with UserId {UserId} tried to get agency Listings", TagConstants.Inactive, id);
      return Forbid();
    }
    var listings = await _listingQService.GetAgencyListings(brokerTuple.Item1.AgencyId, includeSold == 1 ? true : false);

    if (listings == null || !listings.Any()) return NotFound();

    var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
    foreach (var listing in listings)
    {
      listing.DateOfListing = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, listing.DateOfListing);
    }
    //var respnse = new AgencyListingsDTO { listings = listings };
    return Ok(listings);
  }

  /// <summary>
  /// IMPORTANT: Z after Datetime means kind.UTC
  /// </summary>
  /// <param name="dto"></param>
  /// <returns></returns>
  [HttpPost]
  public async Task<IActionResult> CreateAgencyListing([FromBody] CreateListingRequestDTO dto)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item3 || !brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive or non-admin mofo User with UserId {UserId} tried to get Listings", TagConstants.Inactive, id);
      return Forbid();
    }

    var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
    var localDateOfListing = dto.DateOfListing;
    dto.DateOfListing = MyTimeZoneConverter.ConvertToUTC(timeZoneInfo, dto.DateOfListing);

    var listing = await _listingQService.CreateListing(brokerTuple.Item1.AgencyId, dto,id);
    listing.DateOfListing = localDateOfListing;
    return Ok(listing);
  }

  /// <summary>
  /// 
  /// </summary>
  /// <returns>listings assigned to broker only. If admin calls it, will return listings that are assigned
  /// explicitly to him, not all agency's listings
  /// associatedto other brokers</returns>
  [HttpGet("MyListings")]
  public async Task<IActionResult> GetBrokerListings(int includeSold)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive mofo User with UserId {UserId} tried to get broker's Listings", TagConstants.Inactive, id);
      return Forbid();
    }
    var listings = await _brokerTagsQService.GetBrokersListings(id);
    
    if (listings == null || !listings.Any()) return NotFound();

    var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
    foreach (var listing in listings)
    {
      listing.DateOfListing = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, listing.DateOfListing);
    }
    return Ok(listings);
  }

  [HttpPatch("{listingid}")]
  public async Task<IActionResult> EditListing([FromBody] EditListingDTO dto ,int listingid)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item2 || !brokerTuple.Item3)
    {
      _logger.LogWarning("[{Tag}] inactive or non-admin mofo User with UserId {UserId} tried to edit Listings", TagConstants.Inactive, id);
      return Forbid();
    }
    await _listingQService.EditListingAsync(listingid,dto);
    return Ok();
  }


  [HttpPost("AssignToBroker/{listingid}/{brokerid}")]
  public async Task<IActionResult> AssignListingToBroker(int listingid, Guid brokerId)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item3 || !brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive or non-admin mofo User with UserId {UserId} tried to get Listings", TagConstants.Inactive, id);
      return Forbid();
    }

    await _listingQService.AssignListingToBroker(listingid, brokerId, id);
    return Ok();
  }

  [HttpDelete("DetachFromBroker/{listingid}/{brokerid}")]
  public async Task<IActionResult> DetachListingFromBroker(int listingid, Guid brokerId)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item3 || !brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive or non-admin mofo User with UserId {UserId} tried to detach Listing from broker", TagConstants.Inactive, id);
      return Unauthorized();
    }

    await _listingQService.DetachBrokerFromListing(listingid, brokerId, id);

    return Ok();
  }

  [HttpDelete("{listingid}")]
  public async Task<IActionResult> DeleteListing(int listingid)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item3 || !brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive or non-admin mofo User with UserId {UserId} tried to detach Listing from broker", TagConstants.Inactive, id);
      return Unauthorized();
    }

    await _listingQService.DeleteAgencyListingAsync(listingid,brokerTuple.Item1.AgencyId,id);

    return Ok();
  }
}
