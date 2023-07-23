using Azure;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Net.Http.Headers;
using System.Text.Json;
using Web.ApiModels.RequestDTOs.Google;
using Web.Config;

namespace Web.Api.TestingAPI;

[DevOnly]
[Route("api/[controller]")]
public class TestGmailController : ControllerBase
{
    private readonly ILogger<TestGmailController> _logger;
    private readonly AppDbContext _dbcontext;
    private static readonly string HeaderKeyName = "X-Requested-With";
    private static readonly string HeaderValue = "XmlHttpRequest";
    private static string currentRefreshToken = "1//01TYIZZM6-jeUCgYIARAAGAESNwF-L9IrH62JyJKdlfY6tLcV2hn3sqJ2iclDdEHVKa7koHfuyiUAMqHRelD2-dd2wKB8Q8bJI44";
    public TestGmailController(ILogger<TestGmailController> logger, AppDbContext appDbContext)
    {
        _logger = logger;
        _dbcontext = appDbContext;
    }


    [HttpGet("refresh")]
    public async Task<IActionResult> refreshAsync()
    {
        var acces_token = await RefreshAccessTokenAsync1(currentRefreshToken);
        return Ok(acces_token);
    }

    [HttpPost]
    public async Task<IActionResult> PostQueryAsync([FromBody] CodeSendingDTO dto)
    {

        if (!Request.Headers.TryGetValue(HeaderKeyName, out StringValues headerValue) || headerValue != HeaderValue)
        {
            return BadRequest();
        }

        var acces_token = "";
        using (StreamReader reader = new StreamReader(Request.Body))
        {
            string content = await reader.ReadToEndAsync();
            Console.WriteLine(content);

            var code = dto.code;
            Console.WriteLine("code: " + code);
            //using StringContent jsonContent = new(
            //    JsonSerializer.Serialize(new
            //    {
            //        code = code,
            //        client_id = "912588585432-t1ui7blfmetvff3rmkjjjv19vf8pdouj.apps.googleusercontent.com",
            //        client_secret = "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",
            //        grant_type = "authorization_code",
            //        redirect_uri = "https://localhost:7212/WeatherForecast/code",
            //    }),
            //    Encoding.UTF8,
            //    "application/x-www-form-urlencoded");

            var data = new[]
            {
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("client_id", "912588585432-t1ui7blfmetvff3rmkjjjv19vf8pdouj.apps.googleusercontent.com"),
                    new KeyValuePair<string, string>("client_secret", "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"),
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("redirect_uri", @"http://localhost:3000")
            };

            var _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://oauth2.googleapis.com/token");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = null;
            try
            {
                response = await _httpClient.PostAsync("", new FormUrlEncodedContent(data));
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var dto1 = JsonSerializer.Deserialize<GoogleResDTO>(jsonResponse);
                _logger.LogInformation(dto1.access_token);
                _logger.LogInformation(dto1.expires_in.ToString());
                _logger.LogInformation(dto1.token_type);
                _logger.LogInformation(dto1.scope);
                _logger.LogInformation(dto1.refresh_token);
                acces_token = dto1.access_token;
            }
            catch (Exception ex)
            {
                var ess = ex;
            }

            var llol = response;
        }

        var url = "https://gmail.googleapis.com/gmail/v1/users/me/profile?key=" + "AIzaSyCWMcBYvbuNCqpQmhHuC-xyQ4J3Vy0ejuw";
        var _httpClient1 = new HttpClient();
        _httpClient1.BaseAddress = new Uri(url);
        _httpClient1.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acces_token);
        var response2 = await _httpClient1.GetAsync("");
        var jsonResponse2 = await response2.Content.ReadAsStringAsync();

        var return1 = new { acces_token };
        return Ok(return1);
    }

    /// <summary>
    /// refresh token and return new access token
    /// </summary>
    /// <returns></returns>
    private async Task<string?> RefreshAccessTokenAsync1(string refresToken)
    {
        var endpoint = "https://oauth2.googleapis.com/token";
        var data = new[]
            {
                    new KeyValuePair<string, string>("refresh_token", refresToken),
                    new KeyValuePair<string, string>("client_id", "912588585432-t1ui7blfmetvff3rmkjjjv19vf8pdouj.apps.googleusercontent.com"),
                    new KeyValuePair<string, string>("client_secret", "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"),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
            };

        var _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(endpoint);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        HttpResponseMessage response = null;
        try
        {
            response = await _httpClient.PostAsync("", new FormUrlEncodedContent(data));
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var dto1 = JsonSerializer.Deserialize<GoogleResDTO>(jsonResponse);
            _logger.LogInformation("regfreshing acces token");
            _logger.LogInformation(dto1.access_token);
            _logger.LogInformation(dto1.expires_in.ToString());
            _logger.LogInformation(dto1.token_type);
            _logger.LogInformation(dto1.scope);
            return dto1.access_token;
        }
        catch (Exception ex)
        {
            var ess = ex;
            return null;
        }
    }
    //public class CodeSendingDTO
    //{
    //    public string code { get; set; }
    //}
    //public class GoogleResDTO
    //{
    //    public string access_token { get; set; }
    //    public int expires_in { get; set; }
    //    public string refresh_token { get; set; }
    //    public string scope { get; set; }
    //    public string token_type { get; set; }
    //}
}