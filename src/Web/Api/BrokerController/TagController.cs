using Core.Config.Constants.LoggingConstants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;

namespace Web.Api.BrokerController;

[Authorize]
public class TagController : BaseApiController
{
    private readonly ILogger<TagController> _logger;
    private readonly TagQService _TagQService;
    public TagController(AuthorizationService authorizeService, IMediator mediator,
      TagQService tagQService,
      ILogger<TagController> logger) : base(authorizeService, mediator)
    {
        _logger = logger;
        _TagQService = tagQService;
    }


    [HttpPost("{tagname}")]
    public async Task<IActionResult> CreateTag(string tagname)
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive mofo User", TagConstants.Inactive);
            return Forbid();
        }
        var tagDTO = await _TagQService.CreateBrokerTagAsync(id, tagname);
        return Ok(tagDTO);
    }

    [HttpGet]
    public async Task<IActionResult> GetTags()
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive mofo User", TagConstants.Inactive);
            return Forbid();
        }

        var tags = await _TagQService.GetBrokerTagsAsync(id);
        if (tags == null) return NotFound();
        return Ok(tags);
    }

    [HttpPost("{TagId}/{LeadId}")]
    public async Task<IActionResult> AttachToLead(int TagId, int LeadId)
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive or non-admin mofo User", TagConstants.Inactive);
            return Forbid();
        }

        await _TagQService.TagLeadAsync(LeadId, TagId, id);
        return Ok();
    }

    [HttpDelete("{TagId}/{LeadId}")]
    public async Task<IActionResult> DetachFromLead(int TagId, int LeadId)
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive or non-admin mofo User", TagConstants.Inactive);
            return Forbid();
        }

        await _TagQService.DeleteTagFromLeadAsync(LeadId, TagId, id);
        return Ok();
    }
}
