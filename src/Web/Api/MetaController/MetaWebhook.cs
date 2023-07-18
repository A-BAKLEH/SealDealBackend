//using Microsoft.AspNetCore.Mvc;

//namespace Web.Api.MetaController;

//[Route("api/[controller]")]
//[ApiController]
//public class MetaWebhook : ControllerBase
//{
//    private readonly ILogger<MetaWebhook> _logger;
//    string Pagetoken = "EAACfuQLYlt8BANsbcT4ZCenLEhqUhPCDZC9GIgyYUhGBWivUA06E7w5MCMRLHUruhH6mzhNRdpZC9WxD4SZCYK6SDfwnrkcZA6eL7a2fG0ZCsdPIvpRVJ1ZCxIM0gpzuzyju5Fers9zZCaW3aPluianx8YWt4e8PtqBu79J87d19Xcp8GaX6AK5j";
//    public MetaWebhook(ILogger<MetaWebhook> logger)
//    {
//        _logger = logger;
//    }

//    [HttpPost]
//    public async Task<IActionResult> PostWebhook([FromQuery] string validationToken = null)
//    {
//        using (StreamReader reader = new StreamReader(Request.Body))
//        {
//            string content = await reader.ReadToEndAsync();
//            _logger.LogInformation("content :\n\n" + content);
//            return Ok();
//        }
//    }

//    [HttpGet]
//    public async Task<IActionResult> GetWebhook([FromQuery(Name = "hub.mode")] string mode, [FromQuery(Name = "hub.verify_token")] string verify_token, [FromQuery(Name = "hub.challenge")] string challenge)
//    {
//        string Mytoken = "tokenToVerifyWebhook";
//        _logger.LogInformation("mode: {mode} ,token: {token} , challenge: {challenge}", mode, verify_token, challenge);
//        if (mode == "subscribe" && Mytoken == verify_token)
//        {
//            _logger.LogInformation("sending ok");
//            return Ok(challenge);
//        }
//        else
//        {
//            return BadRequest();
//        }
//    }
//}