using Core.Config.Constants.LoggingConstants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Web.ApiModels.RequestDTOs.AINurturing;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;
using Web.ControllerServices.StaticMethods;

namespace Web.Api.AINurturingControllers
{
    [Authorize]
    public class AINurturingController : BaseApiController
    {
        private readonly ILogger<AINurturingController> _logger;
        private readonly AINurturingQService _aiNurturingService;

        public AINurturingController(AuthorizationService authorizeService, AINurturingQService aiNurturingService, IMediator mediator, ILogger<AINurturingController> logger) : base(authorizeService, mediator)
        {
            _logger = logger;
            _aiNurturingService = aiNurturingService;
        }

        [HttpPost("StartNurturing")]
        public async Task<IActionResult> StartNurturing([FromBody] StartNurturingDTO dto)
        {
            var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var brokerTuple = await this._authorizeService.AuthorizeUser(id);
            if (!brokerTuple.Item2)
            {
                _logger.LogCritical("{tag} inactive User", TagConstants.Inactive);
                return Forbid();
            }
            var res = await _aiNurturingService.StartAINurturing(id, dto);

            return Ok(res);
        }

        [HttpPatch("Lead/StopNurturing")]
        public async Task<IActionResult> StopNurturing(int leadId)
        {
            var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var brokerTuple = await this._authorizeService.AuthorizeUser(id);
            if (!brokerTuple.Item2)
            {
                _logger.LogCritical("{tag} inactive User", TagConstants.Inactive);
                return Forbid();
            }

            await _aiNurturingService.StopAINurturing(leadId);

            return Ok();
        }

        [HttpGet("AINurturings")]
        public async Task<IActionResult> GetMyNurturings()
        {
            var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var brokerTuple = await this._authorizeService.AuthorizeUser(id);
            if (!brokerTuple.Item2)
            {
                _logger.LogCritical("{tag} inactive mofo User", TagConstants.Inactive);
                return Forbid();
            }

            var result = await _aiNurturingService.GetMyAINurturingsAsync(id);
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);

            foreach (var item in result)
            {
                item.TimeCreated = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, item.TimeCreated);
            }

            return Ok(result);
        }


        [HttpGet("LeadNurturings")]
        public async Task<IActionResult> GetLeadNurturings(int leadId)
        {
            var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var brokerTuple = await this._authorizeService.AuthorizeUser(id);
            if (!brokerTuple.Item2)
            {
                _logger.LogCritical("{tag} inactive mofo User", TagConstants.Inactive);
                return Forbid();
            }

            var result = await _aiNurturingService.GetLeadAINurturingsAsync(leadId);
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);

            foreach (var item in result)
            {
                item.TimeCreated = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, item.TimeCreated);
            }

            return Ok(result);
        }
    }
}
