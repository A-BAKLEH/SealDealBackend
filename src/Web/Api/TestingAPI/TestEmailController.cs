using Core.Constants;
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

        //try
        //{
        //    var newCat = new OutlookCategory { DisplayName = APIConstants.SentBySealDeal };
        //    newCat.Color = CategoryColor.Preset7;
        //    await _adGraphWrapper._graphClient.Users["bashar.eskandar@sealdeal.ca"].Outlook.MasterCategories.PostAsync(newCat);
        //}
        //catch(ODataError er)
        //{
        //    var err = er;
        //}

        //var message = new Message
        //{
        //    Subject = "wlak",
        //    Body = new ItemBody
        //    {
        //        ContentType = BodyType.Text,
        //        Content = "test body helloooo"
        //    },
        //    ToRecipients = new List<Recipient>()
        //    {
        //        new Recipient
        //        {
        //            EmailAddress = new EmailAddress
        //        {
        //          Address = "basharo9999@hotmail.com"
        //        }
        //      }
        //    },
        //    SingleValueExtendedProperties = new()
        //    {
        //      new SingleValueLegacyExtendedProperty
        //      {
        //        Id = APIConstants.APSentEmailExtendedPropId,
        //        Value = "123x321"
        //      }
        //    },
        //    Categories = new List<string>()
        //    {
        //          APIConstants.SentBySealDeal
        //    }
        //};

        //var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        //{ Message = message, SaveToSentItems = true };

        //await _adGraphWrapper._graphClient.Users["bashar.eskandar@sealdeal.ca"]
        //    .SendMail.PostAsync(requestBody);
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

        //try
        //{
        //    var batchRequestContent = new BatchRequestContent(_adGraphWrapper._graphClient);

        //    int counter = 1;
        //    foreach (var message in failedMessages)
        //    {
        //        var messageUpdateRequest = new Message
        //        {
        //            Id = message.Id,
        //            SingleValueExtendedProperties = new()
        //            {
        //              new SingleValueLegacyExtendedProperty
        //              {
        //                Id = APIConstants.ReprocessMessExtendedPropId,
        //                Value = "1"
        //              }
        //            }
        //        };
        //        batchRequestContent.AddBatchRequestStep(new BatchRequestStep(counter.ToString(), new HttpRequestMessage(HttpMethod.Patch, $"https://graph.microsoft.com/v1.0/users/\"bashar.eskandar@sealdeal.ca\"/messages/{message.Id}")
        //        {
        //            Content = new StringContent(JsonSerializer.Serialize(messageUpdateRequest), Encoding.UTF8, "application/json")
        //        }));
        //        counter++;
        //    }
        //    await _adGraphWrapper._graphClient.Batch.PostAsync(batchRequestContent);
        //}
        //catch (ODataError er)
        //{
        //    _logger.LogError("{place} failed with error code {code} and error message {message}", "TagFailedMessages", er.Error.Code, er.Error.Message);
        //}


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


        //--------
        //int count = 0;
        //int pauseAfter = pagesize;
        //List<Message> messagesList = new(pagesize);

        //var pageIterator = PageIterator<Message, MessageCollectionResponse>
        //    .CreatePageIterator(
        //    _adGraphWrapper._graphClient,
        //    messages,
        //        (m) =>
        //        {
        //            messagesList.Add(m);
        //            count++;
        //            // If we've iterated over the limit,
        //            // stop the iteration by returning false
        //            return count < pauseAfter;
        //        },
        //        (req) =>
        //        {
        //            // Re-add the header to subsequent requests
        //            req.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"", "outlook.body-content-type=\"text\"" });
        //            return req;
        //        }
        //    );
        //await pageIterator.IterateAsync();

        //do
        //{
        //    var lol = messagesList;

        //    // Reset count and list
        //    count = 0;
        //    messagesList = new(pagesize);
        //    await pageIterator.ResumeAsync();
        //} while (pageIterator.State != PagingState.Complete);


        //-------------
        //while (pageIterator.State != PagingState.Complete)
        //{
        //    //process the messages
        //    var lol = messagesList;

        //    // Reset count and list
        //    count = 0;
        //    messagesList = new(pagesize);
        //    await pageIterator.ResumeAsync();
        //}
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
              //config.QueryParameters.Orderby = new string[] { "receivedDateTime desc" };
              config.QueryParameters.Top = 5;
          });
        var replies1 = replies.Value;
        return Ok(replies1);
    }

    [HttpGet("GetMessages/{index}/{isNo}")]
    public async Task<IActionResult> GetMessages(int index, bool isNo)
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
              config.QueryParameters.Top = 4;
              config.Headers.Add("Prefer", new string[] { "IdType=\"ImmutableId\"", "outlook.body-content-type=\"text\"" });
          });
        var messages = messages1.Value;
        var text = messages[index].Body.Content;

        //HtmlDocument doc = new HtmlDocument();
        //doc.LoadHtml(firstMessageContent);
        //string text = doc.DocumentNode.InnerText; // copy paste stripped text

        var lenggg = text.Length;

        //var prompt = APIConstants.ParseLeadPrompt + text;
        //var prompt = APIConstants.ParseLeadPrompt + text;
        //if (isNo) prompt = APIConstants.ParseLeadPrompt + "Hello, my name is abdul, are you interested in our new lead tracking software? let me know thank you! ---Lead provider---- abdul: abdul@hotmail.com, 514 522 5142";

        //var httpClient = new HttpClient()
        //{
        //    BaseAddress = new Uri("https://api.openai.com/v1/chat/completions")
        //    //BaseAddress = new Uri("https://api.openai.com/v1/completions")
        //};
        //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", santaBro);
        //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //StringContent jsonContent = new(
        //JsonSerializer.Serialize(new
        //{
        //    model = "gpt-3.5-turbo",
        //    //model = "text-curie-001",
        //    //prompt = prompt,
        //    messages = new List<GPTRequest>
        //    {
        //        new GPTRequest{role = "user", content = prompt},
        //    },
        //    temperature = 0,
        //}),
        //Encoding.UTF8,
        //"application/json");

        //HttpResponseMessage response = await httpClient.PostAsync("", content: jsonContent);
        //response.EnsureSuccessStatusCode();
        ////check if answer is no before deserialising
        //var jsonResponse = await response.Content.ReadAsStringAsync();

        //var rawResponse = JsonSerializer.Deserialize<GPT35RawResponse>(jsonResponse);

        //var cleanedGOAT = rawResponse.choices[0].message.content.Replace("\n", "");
        //var GOATpart = JsonSerializer.Deserialize<LeadParsingContent>(cleanedGOAT); ;
        //var resWithTime = new { jsonResponse, GOATpart };


        string prompt = APIConstants.ParseLeadPrompt2 + text;

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
        HttpResponseMessage response = await _httpClient.PostAsync("", content: jsonContent);

        //TODO handle API error 
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var rawResponse = JsonSerializer.Deserialize<GPT35RawResponse>(jsonResponse);
        var GPTCompletionJSON = rawResponse.choices[0].message.content.Replace("\n", "");
        var LeadParsed = JsonSerializer.Deserialize<LeadParsingContent>(GPTCompletionJSON);

        return Ok();
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
        DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);
        string emailBash = "bashar.eskandar@sealDeal.ca";
        var Newsubs = new Subscription
        {
            NotificationUrl = url
        };
        VariousCons.MainAPIURL = url;
        //"a3de7de9-3285-4672-bcbb-d18e5e2cb153"
        _adGraphWrapper.CreateClient(tenantId);
        var Subs = await _adGraphWrapper._graphClient.Subscriptions[SubsId].PatchAsync(Newsubs);
        return Ok(Subs);
    }

    [HttpGet("CurrentMainAPIURL")]
    public async Task<IActionResult> GetURL()
    {

        var url = VariousCons.MainAPIURL;
        return Ok(url);
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


        //var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
        //DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);
        //string emailBash = "bashar.eskandar@sealDeal.ca";

        //"a3de7de9-3285-4672-bcbb-d18e5e2cb153"
        _adGraphWrapper.CreateClient(tenantId);
        foreach (var sub3 in subs1)
        {
            await _adGraphWrapper._graphClient.Subscriptions[sub3.Id].DeleteAsync();
        }

        var connectedEmails = await appDbContext1.ConnectedEmails
            .Where(e => e.BrokerId == Guid.Parse("6AB56C6E-5F28-4E60-B9B2-D01C3A8FC314"))
            .ToListAsync();
        foreach (var e in connectedEmails)
        {
            e.SubsExpiryDate = null;
            e.FirstSync = null;
            e.LastSync = null;
            e.SubsExpiryDate = null;
            e.SyncScheduled = false;
            e.SyncJobId = null;
        }
        await appDbContext1.SaveChangesAsync();
        return Ok();



    }

    [HttpGet("ResetConnectedEmailSubs")]
    public async Task<IActionResult> ResetConnectedEmailSubs()
    {
        var tenantId = "d0a40b73-985f-48ee-b349-93b8a06c8384";
        DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);

        await _mSFTEmailQService.DummyMethodHandleAdminConsentAsync(tenantId, Guid.Parse("6AB56C6E-5F28-4E60-B9B2-D01C3A8FC314"), 56);
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
