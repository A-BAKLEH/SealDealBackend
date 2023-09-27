using Core.Config.Constants.LoggingConstants;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Graph.Models.ODataErrors;
using System.Security.Claims;
using TimeZoneConverter;
using Web.ApiModels.RequestDTOs;
using Web.ApiModels.RequestDTOs.Google;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;
using Web.ControllerServices.StaticMethods;

namespace Web.Api.SigninSignup;

[Authorize]
public class AccountController : BaseApiController
{
    private readonly ILogger<AccountController> _logger;
    private readonly MSFTEmailQService _MSFTEmailQService;
    private readonly BrokerQService _brokerQService;
    private readonly StripeQService _stripeQService;
    private readonly MyGmailQService _gmailservice;
    private readonly IHttpClientFactory _httpClientFactory;
    const string HeaderKeyName = "X-Requested-With";
    const string HeaderValue = "XmlHttpRequest";
    private readonly IConfigurationSection _GmailSection;
    public AccountController(AuthorizationService authorizeService,
      IMediator mediator,
      ILogger<AccountController> logger,
      MSFTEmailQService mSFTEmailQService,
      BrokerQService brokerQService,
      StripeQService stripeQService,
      MyGmailQService gmailservice,
      IHttpClientFactory httpClientFactory,
      IConfiguration config) : base(authorizeService, mediator)
    {
        _logger = logger;
        _MSFTEmailQService = mSFTEmailQService;
        _brokerQService = brokerQService;
        _stripeQService = stripeQService;
        _gmailservice = gmailservice;
        _httpClientFactory = httpClientFactory;
        _GmailSection = config.GetSection("Gmail");
    }

    [HttpDelete("DisconnectMsft/{email}")]
    public async Task<IActionResult> DisconnectMsft(string email)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive", TagConstants.Inactive);
            return Forbid();
        }
        await _MSFTEmailQService.DisconnectEmailMsftAsync(id, email);

        return Ok();
    }

    [HttpDelete("DisconnectGmail/{email}")]
    public async Task<IActionResult> DisconnectGmail(string email)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive", TagConstants.Inactive);
            return Forbid();
        }

        await _gmailservice.DisconnectGmailAsync(id, email);
        return Ok();
    }

    [HttpGet("StripeInvoices")]
    public async Task<IActionResult> StripeInvoices()
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2 || !brokerTuple.Item3)
        {
            _logger.LogCritical("{tag} inactive or non-Admin User", TagConstants.Inactive);
            return Forbid();
        }
        var dto = await _stripeQService.GetInvoicesAsync(id);
        return Ok(dto);
    }


    [HttpGet("Verify")]
    public async Task<IActionResult> VerifyAccount()
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogWarning("{tag} inactive User", TagConstants.Inactive);
            return Forbid();
        }
        var dto = await this._authorizeService.VerifyAccountAsync(id);
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
        dto.Created = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, dto.Created);
        return Ok(dto);
    }

    /// <summary>
    /// inputs: english, french
    /// </summary>
    /// <returns></returns>
    [HttpPost("Lang/{SetLanguage}")]
    public async Task<IActionResult> changeAccountlanguage(string SetLanguage)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive User", TagConstants.Inactive);
            return Forbid();
        }
        await _brokerQService.SetaccountLanguage(id, SetLanguage);
        return Ok();
    }

    [HttpPost("SetTimeZone")]
    public async Task<IActionResult> SetTimeZone([FromBody] SigninDTO dto)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive User", TagConstants.Inactive);
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
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive User", TagConstants.Inactive);
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
    /// will also check and handle admin consent if its given
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("ConnectedEmail/GmailConnect")]
    public async Task<IActionResult> ConnectEmailGmail([FromBody] CodeSendingDTO dto)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive User", TagConstants.Inactive);
            return Forbid();
        }

        if (!Request.Headers.TryGetValue(HeaderKeyName, out StringValues headerValue) || headerValue != HeaderValue)
        {
            return BadRequest();
        }


        var clientSecrets = new ClientSecrets
        {
            ClientId = _GmailSection["ClientId"],
            ClientSecret = _GmailSection["ClientSecret"]
        };
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = clientSecrets,
        });

        TokenResponse token = await flow.ExchangeCodeForTokenAsync("lol", dto.code, _GmailSection["RedirectUri"], CancellationToken.None);
        UserCredential cred = new UserCredential(flow, "me", token);
        string accessToken = token.AccessToken;
        string refrehToken = token.RefreshToken;

        var service = new GmailService(
            new BaseClientService.Initializer { HttpClientInitializer = cred });

        var profile = await service.Users.GetProfile("me").ExecuteAsync();

        await _gmailservice.ConnectGmailAsync(id, profile.EmailAddress, refrehToken, accessToken);

        var return1 = new { access_token = accessToken };
        return Ok(return1);
    }

    //---------------test------------------


    ///// <summary>
    ///// will also check and handle admin consent if its given
    ///// </summary>
    ///// <param name="dto"></param>
    ///// <returns></returns>
    //[HttpPost("ConnectedEmail/GmailConnect")]
    //public async Task<IActionResult> ConnectEmailGmail([FromBody] CodeSendingDTO dto)
    //{
    //    var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    //    var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
    //    if (!brokerTuple.Item2)
    //    {
    //        _logger.LogCritical("{tag} inactive User", TagConstants.Inactive);
    //        return Forbid();
    //    }

    //    if (!Request.Headers.TryGetValue(HeaderKeyName, out StringValues headerValue) || headerValue != HeaderValue)
    //    {
    //        return BadRequest();
    //    }

    //    var data = new[]
    //        {
    //                new KeyValuePair<string, string>("code", dto.code),
    //                new KeyValuePair<string, string>("client_id", "912588585432-t1ui7blfmetvff3rmkjjjv19vf8pdouj.apps.googleusercontent.com"),
    //                new KeyValuePair<string, string>("client_secret", "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"),
    //                new KeyValuePair<string, string>("grant_type", "authorization_code"),
    //                new KeyValuePair<string, string>("redirect_uri", @"http://localhost:3000")
    //        };

    //    var _httpClient = _httpClientFactory.CreateClient("TokenGmail");

    //    HttpResponseMessage? response = null;
    //    response = await _httpClient.PostAsync("", new FormUrlEncodedContent(data));
    //    var jsonResponse = await response.Content.ReadAsStringAsync();
    //    var dto1 = JsonSerializer.Deserialize<GoogleResDTO>(jsonResponse);

    //    //TODO check if you need the api key
    //    var url = "https://gmail.googleapis.com/gmail/v1/users/me/profile?key=" + "AIzaSyCWMcBYvbuNCqpQmhHuC-xyQ4J3Vy0ejuw";
    //    var _httpClient1 = _httpClientFactory.CreateClient();
    //    _httpClient1.BaseAddress = new Uri(url);
    //    _httpClient1.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    //    _httpClient1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dto1.access_token);
    //    var response2 = await _httpClient1.GetAsync("");
    //    var jsonResponse2 = await response2.Content.ReadAsStringAsync();
    //    var Emaildto = JsonSerializer.Deserialize<GoogleProfileDTO>(jsonResponse2);

    //    await _gmailservice.ConnectGmailAsync(id, Emaildto.emailAddress, dto1.refresh_token, dto1.access_token);

    //    var return1 = new { dto1.access_token };
    //    return Ok(return1);
    //}

    [HttpGet("ConnectedEmail/GmailConnect/AccessToken/{email}")]
    public async Task<IActionResult> GetTokenGmail(string email)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive User", TagConstants.Inactive);
            return Forbid();
        }
        var token = await _gmailservice.GetTokenGmailAsync(id, email);
        var return1 = new { token };
        return Ok(return1);
    }

    /// <summary>
    /// set autoassignleads for admin
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPatch("ConnectedEmail/AutoAssignLeads")]
    public async Task<IActionResult> PatchConnectedEmail([FromBody] ConnectedEmailAutoAssign dto)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive User tried to get agency Listings", TagConstants.Inactive);
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
    //[HttpGet("ConnectedEmail/MSFT/AdminConsent/Verify/{email}")]
    //public async Task<IActionResult> VerifyAdminConsentedMSFT(string email)
    //{
    //    var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    //    var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
    //    if (!brokerTuple.Item2)
    //    {
    //        _logger.LogCritical("{tag} inactive User tried to handle admin consented", TagConstants.Inactive);
    //        return Forbid();
    //    }
    //    var resTuple = await _MSFTEmailQService.HandleAdminConsentedAsync(id, email);

    //    if (resTuple.Item2)
    //    {
    //        return Ok(resTuple.Item1);
    //    }

    //    return NoContent();
    //}


    [HttpGet("ConnectedEmail/MSFT/AdminConsent/{tenantId}")]
    public async Task<IActionResult> VerifyAdminConsentedMSFTdummy(string tenantId)
    {
        //TODO redo this method
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2 || !brokerTuple.Item3)
        {
            _logger.LogCritical("{tag} inactive User tried to handle admin consented", TagConstants.Inactive);
            return Forbid();
        }
        dynamic? res = null;
        try
        {
            res = await _MSFTEmailQService.DummyMethodHandleAdminConsentAsync(tenantId, id, brokerTuple.Item1.AgencyId);
        }
        catch (ODataError err)
        {
            _logger.LogError("{tag} retrying from controller with error {errorMessage}", "handleAdminConsentController", err.Error.Message);
            await Task.Delay(2000);
            res = await _MSFTEmailQService.DummyMethodHandleAdminConsentAsync(tenantId, id, brokerTuple.Item1.AgencyId);
            _logger.LogInformation("{tag} success after retrying from controller", "handleAdminConsentController");
        }
        return Ok(res);
    }

    [HttpGet("ConnectedEmail")]
    public async Task<IActionResult> GetEmailsConnected()
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive User tried to get emails", TagConstants.Inactive);
            return Forbid();
        }
        dynamic emails = await _MSFTEmailQService.GetConnectedEmails(id);

        return Ok(emails);
    }


    [HttpPost("ConnectedEmail/GoogleCalendar")]
    public async Task<IActionResult> ConnectCalendar([FromBody] CodeSendingDTO dto)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive User", TagConstants.Inactive);
            return Forbid();
        }

        if (!Request.Headers.TryGetValue(HeaderKeyName, out StringValues headerValue) || headerValue != HeaderValue)
        {
            return BadRequest();
        }


        var clientSecrets = new ClientSecrets
        {
            ClientId = _GmailSection["ClientId"],
            ClientSecret = _GmailSection["ClientSecret"]
        };
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = clientSecrets,
        });

        TokenResponse token = await flow.ExchangeCodeForTokenAsync("lol", dto.code, _GmailSection["RedirectUri"], CancellationToken.None);
        UserCredential cred = new UserCredential(flow, "me", token);
        string accessToken = token.AccessToken;
        string refrehToken = token.RefreshToken;

        var service = new GmailService(
            new BaseClientService.Initializer { HttpClientInitializer = cred });

        //might not work here with the email if not using gmail already
        var profile = await service.Users.GetProfile("me").ExecuteAsync();

        await _gmailservice.AddGoogleCalendarAsync(id, profile.EmailAddress, refrehToken, accessToken);

        var return1 = new { access_token = accessToken };
        return Ok(return1);
    }

    [HttpPatch("ConnectedEmail/ToggleCalendarSync")]
    public async Task<IActionResult> ToggleCalendarSync([FromBody] ToggleCalendarDTO dto)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive User tried to get agency Listings", TagConstants.Inactive);
            return Forbid();
        }
        await _gmailservice.ToggleCalendarSync(id,dto.email, dto.toggle);

        return Ok();
    }
}
