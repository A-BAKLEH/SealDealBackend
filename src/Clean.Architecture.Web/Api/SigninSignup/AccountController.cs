﻿using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.Web.ApiModels.RequestDTOs;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.ControllerServices.QuickServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeZoneConverter;

namespace Clean.Architecture.Web.Api.SigninSignup;

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

  [HttpPost("Verify")]
  public async Task<IActionResult> VerifyAccount([FromBody] SigninDTO  dto)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);

    var timeZoneId = TZConvert.GetTimeZoneInfo(dto.IanaTimeZone).Id;
    var accountStatus = await this._authorizeService.VerifyAccountAsync(id,timeZoneId);
    
    return Ok(accountStatus);
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
    await _brokerQService.SetTimeZoneAsync(brokerTuple.Item1,timeZoneId);

    return Ok();
  }

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
      await _MSFTEmailQService.ConnectEmail(brokerTuple.Item1, dto.Email, dto.TenantId,false);
    }

    return Ok();
  }

  /// <summary>
  /// handle after admin consented
  /// </summary>
  /// <param name="dto"></param>
  /// <returns></returns>
  [HttpPost("ConnectedEmail/MSFT/AdminConsented")]
  public async Task<IActionResult> AdminConsentedMSFT([FromBody] AdminConsentDTO dto)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive User or non-admin with UserId {UserId} tried to handle admin consented", TagConstants.Inactive, id);
      return Forbid();
    }
    await _MSFTEmailQService.HandleAdminConsented(brokerTuple.Item1, dto.Email, dto.TenantId);

    return Ok();
  }
}
