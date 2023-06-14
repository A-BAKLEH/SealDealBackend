﻿using Core.Constants;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Web.Constants;
using Web.ControllerServices.QuickServices;
using Web.HTTPClients;

namespace Web.Api.TestingAPI;

[Route("api/[controller]")]
[ApiController]
public class TestEmailController : ControllerBase
{
    private readonly ADGraphWrapper _adGraphWrapper;
    private readonly ILogger<TestEmailController> _logger;
    public string santaBro = "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";
    private readonly AppDbContext appDbContext1;
    public readonly MSFTEmailQService _mSFTEmailQService;
    public TestEmailController(ADGraphWrapper aDGraphWrapper,
        MSFTEmailQService mSFTEmailQService, AppDbContext appDbContext, ILogger<TestEmailController> logger)
    {
        _logger = logger;
        _adGraphWrapper = aDGraphWrapper;
        appDbContext1 = appDbContext;
        _mSFTEmailQService = mSFTEmailQService;
    }

    [HttpGet("testemailprops")]
    public async Task<IActionResult> testemailprops()
    {
        var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
        _adGraphWrapper.CreateClient(tenantId);

        var date1 = DateTimeOffset.UtcNow - TimeSpan.FromDays(360);
        var date = date1.ToString("o");
        var test = await _adGraphWrapper._graphClient
        .Users["bashar.eskandar@sealdeal.ca"]
        .MailFolders["Inbox"]
        .Messages
        .GetAsync(config =>
        {
            config.QueryParameters.Filter = $"receivedDateTime gt {date} and sender/emailAddress/address eq 'basharo9999@hotmail.com'";
            config.QueryParameters.Orderby = new string[] { "receivedDateTime Desc" };
            config.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"" });
            config.QueryParameters.Top = 2;
        });

        var failedMessages = test.Value;
        foreach (var m in failedMessages)
        {
            await _adGraphWrapper._graphClient.Users["bashar.eskandar@sealdeal.ca"]
            .Messages[m.Id]
            .PatchAsync(new Message
            {
                SingleValueExtendedProperties = new()
                            {
                              new SingleValueLegacyExtendedProperty
                              {
                                Id = APIConstants.ReprocessMessExtendedPropId,
                                Value = "1"
                              }
                            }
            });
        }

        var final = await _adGraphWrapper._graphClient
        .Users["bashar.eskandar@sealdeal.ca"]
        .MailFolders["Inbox"]
        .Messages
        .GetAsync(config =>
        {
            config.QueryParameters.Filter = $"receivedDateTime gt {date} and sender/emailAddress/address eq 'basharo9999@hotmail.com' and singleValueExtendedProperties/any(ep:ep/id eq '{APIConstants.ReprocessMessExtendedPropId}' and ep/value eq '1')";
            config.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"" });
            config.QueryParameters.Orderby = new string[] { "receivedDateTime Desc" };
            config.QueryParameters.Top = 2;
        });
        var res = final.Value;

        return Ok();
    }
    private async Task<Message?> getTest(int i)
    {
        try
        {
            var replies = await _adGraphWrapper._graphClient
            .Users["bashar.eskandar@sealdeal.ca"]
            .Messages
            .GetAsync(config =>
            {
                config.QueryParameters.Top = 5;
            });
            return replies.Value.First();
        }
        catch (ODataError odataError)
        {
            _logger.LogCritical("error index {i} with errorCode {errorCode} and message {errorMessage}", i, odataError.Error.Code, odataError.Error.Message);
            return null;
        }
    }

    [HttpGet("testConcurrency")]
    public async Task<IActionResult> testConcurrency()
    {
        var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
        _adGraphWrapper.CreateClient(tenantId);

        var tasks = new List<Task<Message?>>(10);

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(getTest(i));
        }
        await Task.WhenAll(tasks);

        var res = tasks.Select(t => t.Result).ToList();
        return Ok();
    }

    [HttpGet("testCConvo")]
    public async Task<IActionResult> testCCCONvo()
    {
        var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
        _adGraphWrapper.CreateClient(tenantId);
        var date1 = DateTimeOffset.UtcNow - TimeSpan.FromDays(200);
        var date = date1.ToString("o");
        var messages1 = await _adGraphWrapper._graphClient
          .Users["bashar.eskandar@sealdeal.ca"]
          .MailFolders["Inbox"]
          .Messages
          .GetAsync(config =>
          {
              config.QueryParameters.Top = 5;
              config.QueryParameters.Select = new string[] { "id", "from", "conversationId", "receivedDateTime" };
              config.QueryParameters.Filter = $"receivedDateTime gt {date}";
              config.QueryParameters.Orderby = new string[] { "receivedDateTime Desc" };
              config.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"" });
          }
          );
        var convId = messages1.Value.First().ConversationId;
        try
        {
            var messages2 = await _adGraphWrapper._graphClient
          .Users["bashar.eskandar@sealdeal.ca"]
          .Messages
          .GetAsync(config =>
          {
              config.QueryParameters.Top = 3;
              config.QueryParameters.Select = new string[] { "id", "from", "conversationId", "receivedDateTime" };
              config.QueryParameters.Filter = $"receivedDateTime gt {date} and conversationId eq '{convId}'";
              config.QueryParameters.Orderby = new string[] { "receivedDateTime" };
              config.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"" });
          }
          );
        }
        catch (ODataError er)
        {
            var eri = er;
        }
        return Ok();
    }

    [HttpGet("convotest")]
    public async Task<IActionResult> convotestg()
    {
        var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";

        var date1 = DateTimeOffset.UtcNow - TimeSpan.FromDays(360);
        var date = date1.ToString("o");
        int pagesize = 4;

        _adGraphWrapper.CreateClient(tenantId);
        var messages = await _adGraphWrapper._graphClient
          .Users["bashar.eskandar@sealdeal.ca"]
          .MailFolders["Inbox"]
          .Messages
          .GetAsync(config =>
          {
              config.QueryParameters.Top = pagesize;
              config.QueryParameters.Select = new string[] { "id", "sender", "from", "subject", "isRead", "conversationId", "receivedDateTime", "body" };
              config.QueryParameters.Filter = $"receivedDateTime gt {date}";
              config.QueryParameters.Orderby = new string[] { "receivedDateTime" };
              config.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"", "outlook.body-content-type=\"text\"" });
          }
          );
        bool first = true;

        do
        {
            if (!first)
            {
                var nextPageRequestInformation = new RequestInformation
                {
                    HttpMethod = Method.GET,
                    UrlTemplate = messages.OdataNextLink,
                };
                nextPageRequestInformation.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"", "outlook.body-content-type=\"text\"" });
                messages = await _adGraphWrapper._graphClient.RequestAdapter.SendAsync(nextPageRequestInformation, (parseNode) => new MessageCollectionResponse());
            }
            first = false;

            //process messages
            var messs = messages.Value;

        } while (messages.OdataNextLink != null);
        return Ok();
    }


    [HttpGet("GraphFilterTest")]
    public async Task<IActionResult> GraphReplyTesting()
    {
        var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";

        _adGraphWrapper.CreateClient(tenantId);
        var messages = await _adGraphWrapper._graphClient
          .Users["bashar.eskandar@sealdeal.ca"]
          .MailFolders["Inbox"]
          .Messages
          .GetAsync(config =>
          {
              config.QueryParameters.Orderby = new string[] { "receivedDateTime desc" };
              config.QueryParameters.Top = 5;
          });
        var messages1 = messages.Value;

        var replies = await _adGraphWrapper._graphClient
          .Users["bashar.eskandar@sealdeal.ca"]
          .MailFolders["SentItems"]
          .Messages
          .GetAsync(config =>
          {
              config.QueryParameters.Filter = $"singleValueExtendedProperties/Any(ep: ep/id eq 'String 0x1042' and ep/value eq '{messages1[4].InternetMessageId}')";
              config.QueryParameters.Top = 5;
          });
        var replies1 = replies.Value;
        return Ok(replies1);
    }

    [HttpGet("GetMessages/{index}")]
    public async Task<IActionResult> GetMessages(int index)
    {
        var SyncStartDate = new DateTime(2023, 1, 1).ToUniversalTime();

        var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";

        _adGraphWrapper.CreateClient(tenantId);

        var messages1 = await _adGraphWrapper._graphClient
          .Users["bashar.eskandar@sealdeal.ca"]
          .MailFolders["Inbox"]
          .Messages
          .GetAsync(config =>
          {
              config.QueryParameters.Orderby = new string[] { "receivedDateTime desc" };
              config.QueryParameters.Top = 15;
              config.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"", "outlook.body-content-type=\"text\"" });
          });
        var messages = messages1.Value;
        var text = messages[index].Body.Content;

        if (index == 10)
        {
            ////Que pensez-vous de ce renvoi de client potentiel?
            //string pattern = @"Que pensez-vous de ce renvoi de client potentiel";
            ////MatchCollection matches = Regex.Matches(input, pattern);
            ////var builder = new StringBuilder(input);
            //var match = Regex.Match(text, pattern);
            //if(match.Success)
            //{
            //    var indexStart = match.Groups[1].Index;
            //    text = text.Substring(0,indexStart);
            //}
            string pattern = @"Que pensez-vous de ce renvoi de client potentiel";
            var indexStart = text.IndexOf(pattern);
            text = text.Substring(0, indexStart);
        }

        string prompt = APIConstants.ParseLeadPrompt3 + text;

        StringContent jsonContent = new(
        JsonSerializer.Serialize(new
        {
            model = "gpt-3.5-turbo",
            messages = new List<GPTRequest>
            {
                new GPTRequest{role = "user", content = prompt},
            },
            temperature = 0,
        }),
        Encoding.UTF8,
        "application/json");
        var _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/chat/completions");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "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");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        HttpResponseMessage response = null;
        try
        {
            response = await _httpClient.PostAsync("", content: jsonContent);
        }
        catch (Exception ex)
        {
            var ess = ex;
        }


        //TODO handle API error 
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var rawResponse = JsonSerializer.Deserialize<GPT35RawResponse>(jsonResponse);
        var GPTCompletionJSON = rawResponse.choices[0].message.content.Replace("\n", "");

        var LeadParsed = JsonSerializer.Deserialize<LeadParsingContent>(GPTCompletionJSON);

        return Ok(jsonResponse);
    }

    [HttpGet("testsupportmail")]
    public async Task<IActionResult> testsupportmail()
    {
        //{
        //    "EmailAddress":{
        //        "Name":"gscales@datarumble.com",
        //    "Address":"gscales@datarumble.com"
        //    }
        //},
        var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
        _adGraphWrapper.CreateClient(tenantId);
        var message = new Message
        {
            Subject = "69",
            From = new Recipient
            {
                EmailAddress = new EmailAddress
                {
                    Address = "support@sealdeal.ca"
                }
            },
            Body = new ItemBody
            {
                ContentType = BodyType.Text,
                Content = "abdul body"
            },
            ToRecipients = new List<Recipient>()
            {
              new Recipient
              {
                  EmailAddress = new EmailAddress
                {
                  Address = "basharo9999@hotmail.com"
                }
              }
            }
        };

        var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        { Message = message, SaveToSentItems = true };

        try
        {
            await _adGraphWrapper._graphClient.Users["support@sealdeal.ca"]
            .SendMail.PostAsync(requestBody);
        }
        catch (ODataError ex)
        {
            var err = ex;
        }

        var sent = await _adGraphWrapper._graphClient.Users["support@sealdeal.ca"]
            .MailFolders["SentItems"]
            .Messages
            .GetAsync();

        return Ok();
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
        var CreatedSubsTask = await _adGraphWrapper._graphClient.Subscriptions[SubsId].PatchAsync(subs);
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
        var Subs = await _adGraphWrapper._graphClient.Subscriptions.GetAsync();
        var subs1 = Subs.Value;
        return Ok(subs1);
    }

    [HttpGet("UpdateSubsURL/{SubsId}/{url}")]
    public async Task<IActionResult> UpdateSubsURL(string SubsId, string url)
    {

        var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";

        var connEmail = await appDbContext1.ConnectedEmails.Where(e => e.tenantId == tenantId).FirstAsync();
        string emailBash = "bashar.eskandar@sealDeal.ca";
        var Newsubs = new Subscription
        {
            NotificationUrl = VariousCons.MainAPIURL,
            ExpirationDateTime = connEmail.SubsExpiryDate

        };
        //VariousCons.MainAPIURL = url;
        //"a3de7de9-3285-4672-bcbb-d18e5e2cb153"
        _adGraphWrapper.CreateClient(tenantId);
        try
        {
            var Subs = await _adGraphWrapper._graphClient.Subscriptions[SubsId].PatchAsync(Newsubs);
        }
        catch (ODataError err)
        {
            var sdf = err;
        }

        return Ok();
    }

    [HttpGet("setconnectedEmailLastSyncdate")]
    public async Task<IActionResult> setconnectedEmailLastSyncdate()
    {
        var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
        var connectedEmails = await appDbContext1.ConnectedEmails
            .Where(e => e.tenantId == tenantId)
            .ToListAsync();
        foreach (var em in connectedEmails)
        {
            em.LastSync = DateTime.UtcNow;
        }
        await appDbContext1.SaveChangesAsync();
        return Ok();
    }


    [HttpDelete("DeleteSubsNow")]
    public async Task<IActionResult> DeleteSubsURL()
    {

        var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
        DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);
        string emailBash = "bashar.eskandar@sealDeal.ca";

        //"a3de7de9-3285-4672-bcbb-d18e5e2cb153"
        _adGraphWrapper.CreateClient(tenantId);
        var Subs = await _adGraphWrapper._graphClient.Subscriptions.GetAsync();
        var subs1 = Subs.Value;

        foreach (var sub3 in subs1)
        {
            await _adGraphWrapper._graphClient.Subscriptions[sub3.Id].DeleteAsync();
        }

        var connectedEmails = await appDbContext1.ConnectedEmails
            .Where(e => e.tenantId == tenantId)
            .ToListAsync();
        foreach (var e in connectedEmails)
        {
            e.SubsExpiryDate = null;
            e.FirstSync = null;
            e.LastSync = null;
            e.SubsExpiryDate = null;
            e.SyncScheduled = false;
            e.SyncJobId = null;
            e.GraphSubscriptionId = null;
        }
        await appDbContext1.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("ResetConnectedEmailSubs")]
    public async Task<IActionResult> ResetConnectedEmailSubs()
    {
        var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
        DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);

        await _mSFTEmailQService.DummyMethodHandleAdminConsentAsync(tenantId, Guid.Parse("F723997C-75C7-4D9C-82B0-D51034028EFA"), 57);
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
            NotificationUrl = VariousCons.MainAPIURL + "/MsftWebhook/Webhook",
            Resource = $"users/{emailBash}/messages"
            //Resource = $"users/{email}/messages
            //TODO test subscribing to all messages and see it it will notify when a folder other than inbox
            //receives email, check if it notifies with sent emails also
        };
        _adGraphWrapper.CreateClient(tenantId);
        try
        {
            var CreatedSubsTask = await _adGraphWrapper._graphClient.Subscriptions.PostAsync(subs);
            _logger.LogWarning("Subscription IDDDDDDDD: {Id}", CreatedSubsTask.Id);
        }
        catch (ServiceException ex)
        {
            if (ex.ResponseStatusCode == 403)
            {
                _logger.LogWarning("gottem");
            }
        }


        return Ok();
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

        //List<Option> options = EmailHelpers.GetDeltaQueryOptions(SyncStartDate);
        //options.AddDeltaHeaderOptions();

        //IMessageDeltaCollectionPage messages = await _adGraphWrapper._graphClient
        //  .Users["bashar.eskandar@sealdeal.ca"]
        //  .MailFolders["Inbox"]
        //  .Messages
        //  .Delta()
        //  .Request(options)
        //  .GetAsync();

        //EmailHelpers.ProcessMessages(messages, _logger);

        //while (messages.NextPageRequest != null)
        //{
        //    //verify that header max size exists
        //    messages = messages.NextPageRequest
        //      .Header("Prefer", "IdType=ImmutableId")
        //      .Header("Prefer", "odata.maxpagesize=4")
        //      .GetAsync().Result;
        //    EmailHelpers.ProcessMessages(messages, _logger);
        //}
        //object? deltaLink;
        //if (messages.AdditionalData.TryGetValue("@odata.deltaLink", out deltaLink))
        //{
        //    //inboxSync1.DeltaToken = deltaLink.ToString();
        //    _logger.LogWarning("Delta Link: {delaLink}", deltaLink.ToString());//SAVE ONLy after the "=" sign
        //    this.deltaLinkString = deltaLink.ToString();
        //}
        //else
        //{
        //    //TODO log error
        //}
        return Ok();
    }

    [HttpGet("UseDeltaToken")]
    public async Task<IActionResult> USEToken()
    {

        //    var deltaUri = new Uri("https://graph.microsoft.com/v1.0/users/bashar.eskandar@sealdeal.ca/mailFolders('Inbox')/messages/delta?$deltatoken=-eIqKiyh_pvakWLxQh8qlsGoqbkRQ3J34CjVYD35S2M348VQK6h5YBWc3kbYixyIXru9xGLmCjJ2zljnDaMY9dgx-3ZJVI5Cvu7jwWYY_MPWfpOD1pn1iR-XHq8Xe3OELMd8d3MjMXdM6Mut5k0N1Zs9cyNZvjbI1lgPgesTjQ8nv_DdLINcqHbpnnYT6vETU24pOVMyAflVBJ3M3BJX_LiqpLBHk0H-9RV9Rtda3lwXj29lbpm4Ydn4bHNv_wshyCk267XZMGywys4fWrmQ2g.YBEsspT_LkBsWFxSsjfnIWR3sLrKQtuvACN-FvysZ9M");
        //    var deltaToken = deltaUri.Query.Split("=")[1];//get the second part of the query that its the deltaToken
        //    var queryOptions = new List<Option>()
        //{
        //    new QueryOption("$deltatoken", deltaToken),
        //    new HeaderOption("Prefer", "IdType=ImmutableId"),
        //    new HeaderOption("Prefer", "odata.maxpagesize=4")
        //};

        //    var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
        //    DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);
        //    string emailBash = "bashar.eskandar@sealDeal.ca";
        //    _adGraphWrapper.CreateClient(tenantId);

        //    IMessageDeltaCollectionPage messages = await _adGraphWrapper._graphClient
        //      .Users["bashar.eskandar@sealdeal.ca"]
        //      .MailFolders["Inbox"]
        //      .Messages
        //      .Delta()
        //      .Request(queryOptions)
        //      .GetAsync();

        //    EmailHelpers.ProcessMessages(messages, _logger);


        //    while (messages.NextPageRequest != null)
        //    {
        //        //verify that header max size exists
        //        messages = messages.NextPageRequest
        //          .Header("Prefer", "IdType=ImmutableId")
        //          .Header("Prefer", "odata.maxpagesize=4")
        //          .GetAsync().Result;
        //        EmailHelpers.ProcessMessages(messages, _logger);
        //    }
        //    object? deltaLink;
        //    if (messages.AdditionalData.TryGetValue("@odata.deltaLink", out deltaLink))
        //    {
        //        //inboxSync1.DeltaToken = deltaLink.ToString();
        //        _logger.LogWarning("Delta Link: {delaLink}", deltaLink.ToString());
        //        this.deltaLinkString = deltaLink.ToString();
        //    }
        //    else
        //    {
        //        //TODO log error
        //    }
        return Ok();
    }

}
