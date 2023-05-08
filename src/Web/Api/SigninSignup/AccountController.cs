using Core.Config.Constants.LoggingConstants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeZoneConverter;
using Web.ApiModels.RequestDTOs;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;

namespace Web.Api.SigninSignup;

[Authorize]
public class AccountController : BaseApiController
{
    private readonly ILogger<AccountController> _logger;
    private readonly MSFTEmailQService _MSFTEmailQService;
    private readonly BrokerQService _brokerQService;
    public AccountController(AuthorizationService authorizeService,
      IMediator mediator,
      ILogger<AccountController> logger,
      MSFTEmailQService mSFTEmailQService,
      BrokerQService brokerQService) : base(authorizeService, mediator)
    {
        _logger = logger;
        _MSFTEmailQService = mSFTEmailQService;
        _brokerQService = brokerQService;
    }

    [HttpGet("Verify")]
    public async Task<IActionResult> VerifyAccount()
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var dto = await this._authorizeService.VerifyAccountAsync(id);

        return Ok(dto);
    }

    [HttpPost("SetTimeZone")]
    public async Task<IActionResult> SetTimeZone([FromBody] SigninDTO dto)
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogWarning("[{Tag}] inactive User with UserId {UserId} tried to set TimeZone ", TagConstants.Inactive, id);
            return Forbid();
        }
        var timeZoneId = TZConvert.GetTimeZoneInfo(dto.IanaTimeZone).Id;
        await _brokerQService.SetTimeZoneAsync(brokerTuple.Item1, timeZoneId);

        return Ok();
    }

    /// <summary>
    /// will also check and handle admin consent if its given
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("ConnectedEmail/Connect")]
    public async Task<IActionResult> ConnectEmail([FromBody] ConnectEmailDTO dto)
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogWarning("[{Tag}] inactive User with UserId {UserId} tried to get agency Listings", TagConstants.Inactive, id);
            return Forbid();
        }
        if (dto.EmailProvider == "m")
        {
            var res = await _MSFTEmailQService.ConnectEmail(id, dto.Email, dto.TenantId, dto.AssignLeadsAuto);
            return Ok(res);
        }

        return Ok();
    }

    /// <summary>
    /// set autoassignleads for admin
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPatch("ConnectedEmail/AutoAssignLeads")]
    public async Task<IActionResult> PatchConnectedEmail([FromBody] ConnectedEmailAutoAssign dto)
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogWarning("[{Tag}] inactive User with UserId {UserId} tried to get agency Listings", TagConstants.Inactive, id);
            return Forbid();
        }
        await _MSFTEmailQService.SetConnectedEmailAutoAssign(id, dto.email, dto.autoAssign);

        return Ok();
    }

    /// <summary>
    /// verify if admin consented and handle if its the case for all broker emails that belong to 
    /// the tenant
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpGet("ConnectedEmail/MSFT/AdminConsent/Verify/{email}")]
    public async Task<IActionResult> VerifyAdminConsentedMSFT(string email)
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogWarning("[{Tag}] inactive User with UserId {UserId} tried to handle admin consented", TagConstants.Inactive, id);
            return Forbid();
        }
        var resTuple = await _MSFTEmailQService.HandleAdminConsentedAsync(id, email);

        if (resTuple.Item2)
        {
            return Ok(resTuple.Item1);
        }

        return NoContent();
    }


    [HttpGet("ConnectedEmail/MSFT/AdminConsent/{tenantId}")]
    public async Task<IActionResult> VerifyAdminConsentedMSFTdummy(string tenantId)
    {
        //TODO redo this method
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogWarning("[{Tag}] inactive User with UserId {UserId} tried to handle admin consented", TagConstants.Inactive, id);
            return Forbid();
        }
        var resTuple = await _MSFTEmailQService.DummyMethodHandleAdminConsentAsync(tenantId, id);

        return Ok(resTuple);
    }



    [HttpGet("ConnectedEmail")]
    public async Task<IActionResult> GetEmailsConnected()
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogWarning("[{Tag}] inactive User with UserId {UserId} tried to get emails", TagConstants.Inactive, id);
            return Forbid();
        }
        dynamic emails = await _MSFTEmailQService.GetConnectedEmails(id);

        return Ok(emails);
    }
}
