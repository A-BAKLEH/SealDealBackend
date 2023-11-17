using Core.Config.Constants.LoggingConstants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Web.ApiModels.RequestDTOs.ActionPlans;
using Web.ApiModels.RequestDTOs.AINurturing;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;

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

        [HttpPost("TestNurturing")]
        public async Task<IActionResult> TestNurturing()
        {
            await _aiNurturingService.Test();
            return Ok();
        }
    }
}
