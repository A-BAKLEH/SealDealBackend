using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Hangfire;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Web.Config;
using Web.ControllerServices.QuickServices;
using Web.HTTPClients;
using Web.Processing.EmailAutomation;

namespace Web.Api.TestingAPI;

[DevOnly]
[Route("api/[controller]")]
public class TestGmailController : ControllerBase
{
    private readonly ILogger<TestGmailController> _logger;
    private readonly AppDbContext _dbcontext;
    private readonly OpenAIGPT35Service _GPT35Service;
    private readonly MyGmailQService _myGmail;
    private static readonly string HeaderKeyName = "X-Requested-With";
    private static readonly string HeaderValue = "XmlHttpRequest";

    private static string currentRefreshToken = "1//01TYIZZM6-jeUCgYIARAAGAESNwF-L9IrH62JyJKdlfY6tLcV2hn3sqJ2iclDdEHVKa7koHfuyiUAMqHRelD2-dd2wKB8Q8bJI44";
    public TestGmailController(ILogger<TestGmailController> logger, OpenAIGPT35Service openAIGPT35, AppDbContext appDbContext, MyGmailQService myGmailQService)
    {
        _logger = logger;
        _dbcontext = appDbContext;
        _myGmail = myGmailQService;
        _GPT35Service = openAIGPT35;
    }

    [HttpGet("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var connEmail = await _dbcontext.ConnectedEmails
           .FirstAsync(e => !e.isMSFT);

        await _myGmail.RefreshAccessTokenAsync(connEmail.Email, connEmail.BrokerId, null, CancellationToken.None);
        return Ok();
    }

    [HttpGet("process")]
    public async Task<IActionResult> process()
    {
        //MAKE IT DEV ONLY AGAIN
        //check chatGPT changes

        var connEmail = await _dbcontext.ConnectedEmails
           .FirstAsync(e => e.Email == "nayridurgerianrealty@gmail.com");

        GoogleCredential cred = GoogleCredential.FromAccessToken(connEmail.AccessToken);
        var _GmailService = new GmailService(new BaseClientService.Initializer { HttpClientInitializer = cred });

        //var messRequest = _GmailService.Users.Messages.List("me");
        //messRequest.IncludeSpamTrash = false;
        //messRequest.LabelIds = new string[] { "INBOX" };
        //messRequest.MaxResults = 20;

        //var messagesPage = await messRequest.ExecuteAsync();
          
        var getRequest = _GmailService.Users.Messages.Get("me", "18a33c77be1ba8e6");
        getRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;

        var mess = await getRequest.ExecuteAsync();
        var messages = EmailProcessor.DecodeGmail(new List<Message> {mess}, _logger);

        var res = await _GPT35Service.ParseEmailAsync(null, messages[0], connEmail.Email,"bashar","eskandar",true);

        return Ok();
    }
    

    [HttpGet("deleteGmail")]
    public async Task<IActionResult> deleteGmail()
    {
        var connEmail = await _dbcontext.ConnectedEmails
           .FirstAsync(e => !e.isMSFT);
        await _myGmail.CallUnwatch(connEmail.Email, connEmail.BrokerId);

        var jobIdRefresh = connEmail.TokenRefreshJobId;
        Hangfire.BackgroundJob.Delete(jobIdRefresh);
        BackgroundJob.Delete(connEmail.SyncJobId);
        RecurringJob.RemoveIfExists(connEmail.SubsRenewalJobId);

        _dbcontext.Remove(connEmail);
        await _dbcontext.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("test")]
    public async Task<IActionResult> testAsync()
    {
        var connEmail = await _dbcontext.ConnectedEmails
            .FirstAsync(e => e.Email == "shawarmamonster99@gmail.com");
        var time = "Eastern Standard Time";

        //var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
        //result.TimeCreated = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, result.TimeCreated);

        //var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
        //createToDoTaskDTO.dueTime = MyTimeZoneConverter.ConvertToUTC(timeZoneInfo, createToDoTaskDTO.dueTime);


        GoogleCredential cred = GoogleCredential.FromAccessToken(connEmail.AccessToken);
        var _GmailService = new GmailService(new BaseClientService.Initializer { HttpClientInitializer = cred });
        /*
        var yest = DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
        var yestUnix = yest.ToUnixTimeSeconds();

        var weekAgo = DateTimeOffset.UtcNow - TimeSpan.FromDays(7);
        var weekAgoUnix = weekAgo.ToUnixTimeSeconds();

        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(time);

        var dateAfter = new DateTime(2023, 7, 25, 3, 35, 0) - TimeSpan.FromSeconds(5);
        DateTimeOffset offset = MyTimeZoneConverter.ConvertToUTC(timeZoneInfo, dateAfter);
        var todayEpoch = offset.ToUnixTimeSeconds();

        //SUPPORTS PAGE TOKENS
        var messRequest = _GmailService.Users.Messages.List("me");
        messRequest.IncludeSpamTrash = false;
        messRequest.LabelIds = new string[] { "INBOX" };
        messRequest.MaxResults = 20;
        messRequest.Q = $"category:primary";
        var res = await messRequest.ExecuteAsync();

        var gmailMessages = new List<GmailMessage>(res.Messages.Count);
        var request = new BatchRequest(_GmailService);
        res.Messages.ToList().ForEach(m =>
        {
            var getRequest = _GmailService.Users.Messages.Get("me", m.Id);
            getRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
            request.Queue<GmailMessage>(getRequest,
             (content, error, i, message) =>
             {
                 _logger.LogInformation("i {i}",i);
                 //gmailMessages[i] = content;
                 gmailMessages.Insert(i, content);
             });
        });
        // Execute the batch request
        await request.ExecuteAsync();

        int i = 0;
        gmailMessages.ForEach((m) =>
            {
                var t = DateTimeOffset.FromUnixTimeMilliseconds(m.InternalDate.Value).UtcDateTime;
                var o = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, t);
                _logger.LogInformation("index {index} date {date}", i, o);
                i++;
            });
        
        var bytes1 = WebEncoders.Base64UrlDecode(gmailMessages[0].Payload.Parts.First(p => p.MimeType == "text/plain").Body.Data);
        //var bytes = Convert.FromBase64String(gmailMessages[0].Payload.Parts.First(p => p.MimeType == "text/plain").Body.Data);
        //var decodedNotif = Encoding.UTF8.GetString(bytes);
        var decodedNotif1 = Encoding.UTF8.GetString(bytes1);
        gmailMessages.Sort((x, y) => x.InternalDate.Value.CompareTo(y.InternalDate.Value));
        */
        var replacedText = "this is test emai obody lol";
        var labelsRes = await _GmailService.Users.Labels.List("me").ExecuteAsync();
        var labels = labelsRes.Labels.ToList();
        var sentBySealDeal = labels.FirstOrDefault(l => l.Name == "SealDeal:SentByWorkflow");
        if (sentBySealDeal == null)
        {
            sentBySealDeal = await _GmailService.Users.Labels.Create(new Label
            {
                Name = "SealDeal:SentByWorkflow",
                LabelListVisibility = "labelShow",
                MessageListVisibility = "show"
            }, "me").ExecuteAsync();
        }

        var mailMessage = new System.Net.Mail.MailMessage
        {
            To = { "basharo9999@hotmail.com" },
            Subject = "Welcome test",
            Body = "welcome new workspace user test",
        };

        var mimeMessage = MimeMessage.CreateFromMailMessage(mailMessage);

        var gmailMessage = new Message
        {
            Raw = Encode(mimeMessage)
        };
        var mes = await _GmailService.Users.Messages.Send(gmailMessage, "me").ExecuteAsync();
        await _GmailService.Users.Messages.Modify(new ModifyMessageRequest
        {
            AddLabelIds = new List<string>() { sentBySealDeal.Id },
        }, "me", mes.Id).ExecuteAsync();
        //var textToBytes = Encoding.UTF8.GetBytes(replacedText);
        //var encodedBytes = WebEncoders.Base64UrlEncode(textToBytes);
        //await _GmailService.Users.Messages.Send(new GmailMessage
        //{
        //    LabelIds = new List<string>() { sentBySealDeal.Id },
        //    Payload = new MessagePart
        //    {
        //        MimeType = "text/plain",
        //        Body = new MessagePartBody
        //        {
        //            Data = encodedBytes
        //        },
        //        Headers = new List<MessagePartHeader>()
        //            {
        //                new MessagePartHeader
        //                {
        //                    Name = "To",
        //                    Value = "basharo9999@hotmail.com"
        //                },
        //                new MessagePartHeader
        //                {
        //                    Name = "Subject",
        //                    Value = "test subject lol"
        //                }
        //            }
        //    }
        //}, "me")
        //    .ExecuteAsync();
        return Ok();


        //    var his = connEmail.historyId;
        //    var parsedHis = ulong.Parse(his);

        //    var gmailHistoriesRequest = _GmailService.Users.History
        //        .List("me");
        //    gmailHistoriesRequest.StartHistoryId = ulong.Parse(connEmail.historyId);
        //    gmailHistoriesRequest.LabelId = "INBOX";
        //    gmailHistoriesRequest.MaxResults = 7;
        //    gmailHistoriesRequest.HistoryTypes = UsersResource.HistoryResource.ListRequest.HistoryTypesEnum.MessageAdded;

        //    ListHistoryResponse? histories = null;
        //    try
        //    {
        //        histories = await gmailHistoriesRequest.ExecuteAsync();
        //    }
        //    catch (Exception ex) // todo try expired historyId
        //    {
        //        if (ex is HttpRequestException)
        //        {
        //            _logger.LogWarning("{tag} sync email job failed with error {error}", TagConstants.syncEmail, ex.Message + ": " + ex.StackTrace);
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    bool first = true;
        //    string originalHistoryId = connEmail.historyId;
        //    if (histories == null || histories.History == null || histories.History.Count == 0) return Ok();
        //    do
        //    {
        //        if (!first)
        //        {
        //            gmailHistoriesRequest = _GmailService.Users.History
        //                .List("me");
        //            gmailHistoriesRequest.PageToken = histories.NextPageToken;
        //            gmailHistoriesRequest.StartHistoryId = ulong.Parse(originalHistoryId); //TODO not sure if this token or the updated one
        //            gmailHistoriesRequest.LabelId = "INBOX";
        //            gmailHistoriesRequest.MaxResults = 7;
        //            gmailHistoriesRequest.HistoryTypes = UsersResource.HistoryResource.ListRequest.HistoryTypesEnum.MessageAdded;
        //            histories = await gmailHistoriesRequest.ExecuteAsync();
        //        }
        //        first = false;
        //        //only NEW messages in INBOX since last historyID stored in the databased
        //        var gmailMessagesIDs = histories.History.SelectMany(h => h.MessagesAdded.Select(m => m.Message)).ToList();
        //        //message contains only ID and ThreadId
        //        var request = new BatchRequest(_GmailService);

        //        var gmailMessages = new List<GmailMessage>(gmailMessagesIDs.Count);

        //        gmailMessagesIDs.ForEach(m =>
        //        {
        //            var getRequest = _GmailService.Users.Messages.Get("me", m.Id);
        //            getRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
        //            request.Queue<GmailMessage>(getRequest,
        //             (content, error, i, message) =>
        //             {
        //                 gmailMessages[i] = content;
        //             });
        //        });
        //        // Execute the batch request
        //        await request.ExecuteAsync();
        //        gmailMessages.Sort((x, y) => x.InternalDate.Value.CompareTo(y.InternalDate.Value));


        //        connEmail.historyId = histories.HistoryId.ToString();
        //        var lastMessageTime = gmailMessages.Last().InternalDate.Value;

        //        var LastProcessedTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(lastMessageTime);
        //    }
        //    while (histories.NextPageToken != null);



        //    return Ok();
    }

    public static string Encode(MimeMessage mimeMessage)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            mimeMessage.WriteTo(ms);
            return Convert.ToBase64String(ms.GetBuffer())
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}