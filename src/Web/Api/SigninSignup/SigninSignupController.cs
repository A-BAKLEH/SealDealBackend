using Core.Config.Constants.LoggingConstants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeZoneConverter;
using Web.ApiModels.RequestDTOs;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;
using Web.MediatrRequests.SignupRequests;

namespace Web.Api.SigninSignup;

[Authorize]
public class SigninSignupController : BaseApiController
{
    private readonly ILogger<SigninSignupController> _logger;

    public SigninSignupController(AuthorizationService authorizeService, IMediator mediator,
      ILogger<SigninSignupController> logger,
      MSFTEmailQService emailQService) : base(authorizeService, mediator)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SigninSingup([FromBody] SigninDTO dto)
    {
        var l = User.Claims.ToList();
        var newUserClaim = l.Find(x => x.Type == "newUser");

        var timeZoneId = TZConvert.GetTimeZoneInfo(dto.IanaTimeZone).Id;
        //signin only
        if (newUserClaim == null)
        {
            Guid b2cID = Guid.Parse(l.Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
            var res = await _authorizeService.VerifyAccountAsync(b2cID, timeZoneId, true);
            return Ok(res);
        }
        //signup
        var agencyName = l.Find(x => x.Type == "extension_AgencyName").Value;
        var id = Guid.Parse(l.Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var signinResponseDTO = await _mediator.Send(new SignupRequest
        {
            AgencyName = agencyName,
            givenName = l.Find(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname").Value,
            surName = l.Find(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname").Value,
            b2cId = id,
            email = l.Find(x => x.Type == "emails").Value,
            TimeZoneId = timeZoneId
        });
        _logger.LogWarning("{tag} New Agency Signed up with name {agencyName} and admin B2cId {userId}", TagConstants.AgencySignup, agencyName, id);
        return Ok(signinResponseDTO);
    }
}
