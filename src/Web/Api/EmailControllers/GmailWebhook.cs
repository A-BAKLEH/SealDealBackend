using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using Web.ApiModels.RequestDTOs.Google;
using Web.Processing.EmailAutomation;
using static Web.ApiModels.RequestDTOs.Google.GoogleWebhookDTO;

namespace Web.Api.EmailControllers;

[Route("api/[controller]")]
[ApiController]
public class GmailWebhook : ControllerBase
{
    private readonly ILogger<GmailWebhook> _logger;
    private readonly EmailProcessor _emailProcessor;
    public GmailWebhook(ILogger<GmailWebhook> logger, EmailProcessor emailProcessor)
    {
        _logger = logger;
        _emailProcessor = emailProcessor;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        using (StreamReader reader = new StreamReader(Request.Body))
        {
            string content = await reader.ReadToEndAsync();
            _logger.LogWarning("gmail webhook content:" + content);

            var payload = JsonSerializer.Deserialize<GoogleWebhookDTO>(content);
            var bytes = Convert.FromBase64String(payload.message.data);
            var decodedNotif = Encoding.UTF8.GetString(bytes);
            var data = JsonSerializer.Deserialize<GmailWebhookNotif>(decodedNotif);
            var email = data.emailAddress;
            _logger.LogWarning("gmail webhook content decrypted:" + email);
            await _emailProcessor.CheckEmailSyncAsync(false,gmailEmail: email);
        }
        return Ok();
    }
}
