
using Clean.Architecture.Core.Constants;
using Clean.Architecture.Infrastructure.ExternalServices;
using Clean.Architecture.Web.ControllerServices.StaticMethods;
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
  private string? deltaLinkString = null;
  public TestEmailController(ADGraphWrapper aDGraphWrapper, ILogger<TestEmailController> logger)
  {
    _logger = logger;
    _adGraphWrapper = aDGraphWrapper;
  }
  

  [HttpGet("GetMessages")]
  public async Task<IActionResult> GetMessages()
  {
    var SyncStartDate = new DateTime(2023, 1, 1).ToUniversalTime();

    var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";

    var option = new QueryOption("$filter", $"createdDateTime ge {SyncStartDate.ToString("o")}");
    var option1 = new QueryOption("$orderby","createdDateTime desc");
    _adGraphWrapper.CreateClient(tenantId);

    var options = new List<Option>();
    options.Add(option);
    options.Add(option1);

    var messages = await _adGraphWrapper._graphClient
      .Users["bashar.eskandar@sealdeal.ca"]
      //.MailFolders["Inbox"]
      .Messages
      .Request(options)
      .GetAsync();

    return Ok(messages);
  }

  [HttpGet("RenewSubs/{SubsId}")]
  public async Task<IActionResult> renewsubs(string SubsId)
  {
    var SubsIdGuid = Guid.Parse(SubsId);
    var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
    DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);
    string emailBash = "bashar.eskandar@sealDeal.ca";
    var subs = new Subscription
    {
      ExpirationDateTime = SubsEnds
    };
    //"a3de7de9-3285-4672-bcbb-d18e5e2cb153"
    _adGraphWrapper.CreateClient(tenantId);
    var CreatedSubsTask = await _adGraphWrapper._graphClient.Subscriptions[SubsId].Request().UpdateAsync(subs);
    _logger.LogWarning("IDDDDDDDD: {Id}", CreatedSubsTask.Id);
    return Ok();
  }

  [HttpGet("GetSubs")]
  public async Task<IActionResult> Getsubs()
  {
    
    var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
    DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);
    string emailBash = "bashar.eskandar@sealDeal.ca";

    //"a3de7de9-3285-4672-bcbb-d18e5e2cb153"
    _adGraphWrapper.CreateClient(tenantId);
    var Subs = await _adGraphWrapper._graphClient.Subscriptions.Request().GetAsync();
    return Ok(Subs);
  }

  [HttpGet("UpdateSubsURL/{SubsId}/{url}")]
  public async Task<IActionResult> UpdateSubsURL(string SubsId,string url)
  {

    var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
    DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);
    string emailBash = "bashar.eskandar@sealDeal.ca";
    var Newsubs = new Subscription
    {
      NotificationUrl = url
    };
    VariousCons.MainAPIURL = url;
    //"a3de7de9-3285-4672-bcbb-d18e5e2cb153"
    _adGraphWrapper.CreateClient(tenantId);
    var Subs = await _adGraphWrapper._graphClient.Subscriptions[SubsId].Request().UpdateAsync(Newsubs);
    return Ok(Subs);
  }

  [HttpGet("CurrentMainAPIURL")]
  public async Task<IActionResult> GetURL()
  {

    var url = VariousCons.MainAPIURL;
    return Ok(url);
  }


  [HttpDelete("UpdateSubsURL/{SubsId}")]
  public async Task<IActionResult> DeleteSubsURL(string SubsId)
  {

    var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
    DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);
    string emailBash = "bashar.eskandar@sealDeal.ca";

    //"a3de7de9-3285-4672-bcbb-d18e5e2cb153"
    _adGraphWrapper.CreateClient(tenantId);
    await _adGraphWrapper._graphClient.Subscriptions[SubsId].Request().DeleteAsync();
    return Ok();
  }

  [HttpGet("CreateSubsEmail")]
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
      NotificationUrl = VariousCons.MainAPIURL + "/api/MsftWebhook/Webhook",
      Resource = $"users/{emailBash}/messages"
      //Resource = $"users/{email}/messages
      //TODO test subscribing to all messages and see it it will notify when a folder other than inbox
      //receives email, check if it notifies with sent emails also
    };
    _adGraphWrapper.CreateClient(tenantId);
    var CreatedSubsTask = await _adGraphWrapper._graphClient.Subscriptions.Request().AddAsync(subs);
    _logger.LogWarning("Subscription IDDDDDDDD: {Id}", CreatedSubsTask.Id);
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


  [HttpGet("test-deltaToken")]
  public async Task<IActionResult> TestDeltaToken()
  {

    //var SyncStartDate = new DateTimeOffset(new DateTime(2022,12,20),TimeSpan.Zero);
    var SyncStartDate = new DateTime(2022, 12, 20);
    var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
    DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);
    string emailBash = "bashar.eskandar@sealDeal.ca";
    _adGraphWrapper.CreateClient(tenantId); 

    List<Option> options = EmailHelpers.GetDeltaQueryOptions(SyncStartDate);
    options.AddDeltaHeaderOptions();

    IMessageDeltaCollectionPage messages = await _adGraphWrapper._graphClient
      .Users["bashar.eskandar@sealdeal.ca"]
      .MailFolders["Inbox"]
      .Messages
      .Delta()
      .Request(options)
      .GetAsync();

    EmailHelpers.ProcessMessages(messages, _logger);

    while (messages.NextPageRequest != null)
    {
      //verify that header max size exists
      messages = messages.NextPageRequest
        .Header("Prefer", "IdType=ImmutableId")
        .Header("Prefer", "odata.maxpagesize=4")
        .GetAsync().Result;
      EmailHelpers.ProcessMessages(messages,_logger);
    }
    object? deltaLink;
    if (messages.AdditionalData.TryGetValue("@odata.deltaLink", out deltaLink))
    {
      //inboxSync1.DeltaToken = deltaLink.ToString();
      _logger.LogWarning("Delta Link: {delaLink}", deltaLink.ToString());//SAVE ONLy after the "=" sign
      this.deltaLinkString = deltaLink.ToString();
    }
    else
    {
      //TODO log error
    }
    return Ok();
  }

  [HttpGet("UseDeltaToken")]
  public async Task<IActionResult> USEToken()
  {

    var deltaUri = new Uri("https://graph.microsoft.com/v1.0/users/bashar.eskandar@sealdeal.ca/mailFolders('Inbox')/messages/delta?$deltatoken=-eIqKiyh_pvakWLxQh8qlsGoqbkRQ3J34CjVYD35S2M348VQK6h5YBWc3kbYixyIXru9xGLmCjJ2zljnDaMY9dgx-3ZJVI5Cvu7jwWYY_MPWfpOD1pn1iR-XHq8Xe3OELMd8d3MjMXdM6Mut5k0N1Zs9cyNZvjbI1lgPgesTjQ8nv_DdLINcqHbpnnYT6vETU24pOVMyAflVBJ3M3BJX_LiqpLBHk0H-9RV9Rtda3lwXj29lbpm4Ydn4bHNv_wshyCk267XZMGywys4fWrmQ2g.YBEsspT_LkBsWFxSsjfnIWR3sLrKQtuvACN-FvysZ9M");
    var deltaToken = deltaUri.Query.Split("=")[1];//get the second part of the query that its the deltaToken
    var queryOptions = new List<Option>()
    {
        new QueryOption("$deltatoken", deltaToken),
        new HeaderOption("Prefer", "IdType=ImmutableId"),
        new HeaderOption("Prefer", "odata.maxpagesize=4")
    };

    var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
    DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);
    string emailBash = "bashar.eskandar@sealDeal.ca";
    _adGraphWrapper.CreateClient(tenantId);

    IMessageDeltaCollectionPage messages = await _adGraphWrapper._graphClient
      .Users["bashar.eskandar@sealdeal.ca"]
      .MailFolders["Inbox"]
      .Messages
      .Delta()
      .Request(queryOptions)
      .GetAsync();

    EmailHelpers.ProcessMessages(messages, _logger);

    
    while (messages.NextPageRequest != null)
    {
      //verify that header max size exists
      messages = messages.NextPageRequest
        .Header("Prefer", "IdType=ImmutableId")
        .Header("Prefer", "odata.maxpagesize=4")
        .GetAsync().Result;
      EmailHelpers.ProcessMessages(messages, _logger);
    }
    object? deltaLink;
    if (messages.AdditionalData.TryGetValue("@odata.deltaLink", out deltaLink))
    {
      //inboxSync1.DeltaToken = deltaLink.ToString();
      _logger.LogWarning("Delta Link: {delaLink}", deltaLink.ToString());
      this.deltaLinkString = deltaLink.ToString();
    }
    else
    {
      //TODO log error
    }
    return Ok();
  }

}
