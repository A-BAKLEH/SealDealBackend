using Core.Config.Constants.LoggingConstants;
using Web.ApiModels.APIResponses.Lead;
using Web.ApiModels.RequestDTOs;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;
using Web.ControllerServices.StaticMethods;
using Web.MediatrRequests.LeadRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.ApiModels;
using Core.Domain.LeadAggregate;

namespace Web.Api.LeadController;

[Authorize]
public class LeadController : BaseApiController
{
  private readonly ILogger<LeadController> _logger;
  private readonly LeadQService _leadQService;
  public LeadController(AuthorizationService authorizeService, IMediator mediator,
    ILogger<LeadController> logger, LeadQService leadQService) : base(authorizeService, mediator)
  {
    _logger = logger;
    _leadQService = leadQService;
  }

  [HttpPost]
  public async Task<IActionResult> CreateLead([FromBody] CreateLeadDTO createLeadDTO)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] Inactive User with UserId {UserId} tried to create Lead", TagConstants.Unauthorized, id);
      return Forbid();
    }
    // user needs to be admin if he is assigning a lead to someone other than himself
    if (!createLeadDTO.AssignToSelf && createLeadDTO.AssignToBrokerId != id)
    {
      if (!brokerTuple.Item3)
      {
        _logger.LogWarning("[{Tag}] non-admin User with UserId {UserId} tried to assign lead", TagConstants.Unauthorized, id);
        return Forbid();
      }
    }
    var broker = brokerTuple.Item1;
    var lead = await _mediator.Send(new CreateLeadRequest
    {
      BrokerWhoRequested = brokerTuple.Item1,
      createLeadDTO = createLeadDTO
    });

    var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
    lead.EntryDate = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, lead.EntryDate);

    return Ok(lead);
  }

  //only for leads that are assigned to a broker/admin
  [HttpPatch("{LeadId}")]
  public async Task<IActionResult> UpdateLead([FromBody] UpdateLeadDTO dto, int LeadId)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] Inactive User with UserId {UserId} tried to update Lead", TagConstants.Unauthorized, id);
      return Forbid();
    }
    var lead = await _leadQService.UpdateLeadAsync(LeadId, dto,id);

    var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
    lead.EntryDate = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, lead.EntryDate);
    return Ok(lead);
  }

  //only for leads that are assigned to a broker/admin
  //admin can also delete lead that is not assigned to anybody
  [HttpDelete("{LeadId}")]
  public async Task<IActionResult> DeleteLead( int LeadId)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] Inactive User with UserId {UserId} tried to delete Lead", TagConstants.Unauthorized, id);
      return Forbid();
    }
    await _leadQService.DeleteLeadAsync(LeadId,id,brokerTuple.Item1.isAdmin);
    return Ok();
  }


  /// <summary>
  /// for Allah Lead, gets all lead's info, no paging for now
  /// </summary>
  /// <param name="id">id of the lead</param>
  /// <param name="includeEvents">1 to include Notifs, 0 to not include</param>
  /// <returns></returns>
  [HttpGet("AllahLead/{id}/{includeEvents}")]
  public async Task<IActionResult> GetAllahLead(int id, int includeEvents)
  {
    var brokerid = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(brokerid);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] Inactive User with UserId {UserId} tried to create Lead", TagConstants.Unauthorized, brokerid);
      return Forbid();
    }
    var broker = brokerTuple.Item1;
    var lead = await _mediator.Send(new GetAllahLeadRequest
    {
      AgencyId = brokerTuple.Item1.AgencyId,
      BrokerId = brokerid,
      leadId = id,
      includeNotifs = includeEvents == 1 ? true : false
    });
    if (lead == null) return NotFound();

    var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
    lead.EntryDate = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, lead.EntryDate);
    return Ok(lead);
  }

  /// <summary>
  /// will eventually implement paging
  /// </summary>
  /// <param name="leadid"></param>
  /// <param name="lastid"></param>
  /// <returns></returns>
  [HttpGet("Events/{leadid}/{lastid}")]
  public async Task<IActionResult> GetLeadEvents(int leadid, int lastid)
  {
    var brokerid = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(brokerid);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] Inactive User with UserId {UserId} tried to create Lead", TagConstants.Unauthorized, brokerid);
      return Forbid();
    }

    var notifs = await _mediator.Send(new GetLeadEventsRequest
    {
      BrokerId = brokerid,
      leadId = leadid,
      lastNotifID = lastid
    });
    if (notifs == null || !notifs.Any()) return NotFound();
    var res = new LeadEventsResponseDTO { events = notifs };
    return Ok(res);
  }

  /// <summary>
  /// for leads list, implements paging later, now will just return all leads
  /// </summary>
  /// <returns></returns>
  [HttpGet("MyLeads")]
  public async Task<IActionResult> GetLeads()
  {
    var brokerid = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(brokerid);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] Inactive User with UserId {UserId} tried to create Lead", TagConstants.Unauthorized, brokerid);
      return Forbid();
    }

    var leads = await _leadQService.GetLeadsAsync(brokerid);
    if (leads == null || !leads.Any()) return NotFound();

    var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
    foreach (var lead in leads)
    {
      lead.EntryDate = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, lead.EntryDate);
    }

    return Ok(leads);
  }

}
