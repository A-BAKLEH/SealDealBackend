using Azure.Identity;
using Clean.Architecture.Core.Constants;
using Clean.Architecture.Infrastructure.ExternalServices;
using Clean.Architecture.Web.ControllerServices.QuickServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;

namespace Clean.Architecture.Web.Api.TestingAPI;

[Route("api/[controller]")]
[ApiController]
public class TestEmailController : ControllerBase
{
  //public GraphServiceClient _graphClient;
  private readonly ADGraphWrapper _adGraphWrapper;
  private readonly ILogger<TestEmailController> _logger;
  public TestEmailController(ADGraphWrapper aDGraphWrapper, ILogger<TestEmailController> logger)
  {
    _logger = logger;
    _adGraphWrapper = aDGraphWrapper;

   /*// "Instance": "https://login.microsoftonline.com/",
    //"Domain": "basharo9999hotmail.onmicrosoft.com",
    //"TenantId": "6f64f9eb-73c2-4e0c-b1c6-2bb14c3b2d14",
    //"TenantId": "common",
    //"ClientId": "069395cb-909a-4f2f-8bfc-f4e0265374be",
    //"ClientSecret": "Ujq8Q~mPVcyAiZVoFwjcRIOBj1YRYAYuPepuycdm",
    var scopes = new[] { "https://graph.microsoft.com/.default" };
    var tenantId = "common";
    var clientId = "069395cb-909a-4f2f-8bfc-f4e0265374be";
    var clientSecret = "Ujq8Q~mPVcyAiZVoFwjcRIOBj1YRYAYuPepuycdm";

    // using Azure.Identity;
    //var options = new TokenCredentialOptions
    //{
    //  AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
    //};
    var clientSecretCredential = new ClientSecretCredential(
        tenantId, clientId, clientSecret);

    _graphClient = new GraphServiceClient(clientSecretCredential, scopes);*/
  }

  [HttpGet("renew-subs")]
  public async Task<IActionResult> renewsubs()
  {
    var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
    DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);
    string emailBash = "bashar.eskandar@sealDeal.ca";
    var subs = new Subscription
    {
      ExpirationDateTime = SubsEnds
    };
    _adGraphWrapper.CreateClient(tenantId);
    var CreatedSubsTask = await _adGraphWrapper._graphClient.Subscriptions["a3de7de9-3285-4672-bcbb-d18e5e2cb153"].Request().UpdateAsync(subs);
    _logger.LogWarning("IDDDDDDDD: {Id}", CreatedSubsTask.Id);
    return Ok();
  }

  [HttpGet("test-CreatesubsEmail")]
  public async Task<IActionResult> TestNewSchema()
  {
    var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
    DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);
    string emailBash = "bashar.eskandar@sealDeal.ca";
    var subs = new Subscription
    {
      ChangeType = "created",
      ClientState = VariousCons.MSFtWebhookSecret,
      ExpirationDateTime = SubsEnds,
      NotificationUrl = "https://bf69-104-244-67-183.ngrok.io/api/MsftWebhook/Webhook",
      Resource = $"users/{emailBash}/messages"
      //Resource = $"users/{email}/messages
      //TODO test subscribing to all messages and see it it will notify when a folder other than inbox
      //receives email, check if it notifies with sent emails also
    };
    _adGraphWrapper.CreateClient(tenantId);
    var CreatedSubsTask = await _adGraphWrapper._graphClient.Subscriptions.Request().AddAsync(subs);
    _logger.LogWarning("IDDDDDDDD: {Id}", CreatedSubsTask.Id);
    return Ok();
  }


  [HttpGet]
  public async Task<IActionResult> TestResult()
  {
    var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
    var email = "bashar.eskandar@sealdeal.ca";

    _adGraphWrapper.CreateClient(tenantId);
    /*var folder = await _adGraphWrapper._graphClient
      .Users["bashar.eskandar@sealdeal.ca"]
      .MailFolders["inbox"]
      .Request().GetAsync();*/
    /*var messagesInbox = await _adGraphWrapper._graphClient
      .Users["bashar.eskandar@sealdeal.ca"]
      .MailFolders["inbox"]
      .Messages
      .Request().GetAsync();*/

    var messagesSent = await _adGraphWrapper._graphClient
      .Users["bashar.eskandar@sealdeal.ca"]
      //.MailFolders["AAMkADJmNWUwNzZlLWMxNWQtNGNkMy1iNmY4LWJiOTBkYjk5YjVlYgAuAAAAAAC6hwbZVh5-T6nLJ1FvBfvIAQBZbQVR_TVZTLodSM-vZvBeAAAAAAEJAAA="]
      .MailFolders["inbox"]
      //.Messages
      .Request().GetAsync();
    /*foreach (var folder in folders)
    {
      Console.WriteLine("FOLDERRRR:" + "ID:"+ folder.Id + "\n" + "DisplayName:" +
        folder.DisplayName + "\n" + "ParentFolderID" + folder.ParentFolderId);
    }*/
    //Console.WriteLine("FOLDERRRR:" + "ID:" + folder.Id + "\n" + "DisplayName:" +
    //    folder.DisplayName + "\n" + "ParentFolderID" + folder.ParentFolderId);
    //return Ok(folder);
    return Ok(messagesSent);
  }


}
