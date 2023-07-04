using Core.Constants;
using Core.Domain.BrokerAggregate;
using Core.Domain.BrokerAggregate.EmailConnection;
using Core.Domain.TasksAggregate;
using Hangfire;
using Hangfire.Server;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;
using Web.ApiModels.RequestDTOs.Admin;
using Web.Constants;
using Web.ControllerServices.QuickServices;
using Web.Processing.Analyzer;
using Web.Processing.Cleanup;
using Web.Processing.EmailAutomation;

namespace Web.Api.TestingAPI;

[Route("api/[controller]")]
[ApiController]
public class TestProdController : ControllerBase
{
    private IConfigurationSection section;
    private readonly IConfigurationSection _EmailconfigurationSection;
    private readonly IWebHostEnvironment _webHostEnv;
    private readonly AppDbContext appDbContext;
    private readonly ILogger<TestProdController> _logger;
    private string passwd = "helloHabibi69";
    private readonly ADGraphWrapper _adGraphWrapper;
    private readonly AppDbContext appDb;
    private readonly BrokerQService _brokerTagsQService;
    private readonly EmailProcessor emailProcessor;
    public TestProdController(IConfiguration configuration,
        AppDbContext dbContext,
        ILogger<TestProdController> logger,
        IWebHostEnvironment webHostEnv,
        ADGraphWrapper aDGraphWrapper,
        AppDbContext context,
        BrokerQService brokerQService,
        EmailProcessor _emailProcessor)
    {
        section = configuration.GetSection("Hangfire");
        _EmailconfigurationSection = configuration.GetSection("URLs");
        _webHostEnv = webHostEnv;
        appDbContext = dbContext;
        _logger = logger;
        _adGraphWrapper = aDGraphWrapper;
        appDb = context;
        _brokerTagsQService = brokerQService;
        emailProcessor = _emailProcessor;
    }

    [HttpGet("liveLol/{key}")]
    public async Task<IActionResult> livelol(string key)
    {
        if (key != passwd) return Ok("nope");
        return Ok("liveLol");
    }

    [HttpGet("procCount/{key}")]
    public async Task<IActionResult> procCount(string key)
    {
        if (key != passwd) return Ok("nope");
        var count = Environment.ProcessorCount;
        return Ok(count);
    }

    [HttpPost("SetControl")]
    public async Task<IActionResult> SetControl([FromBody] ControlDTO dto)
    {
        if (dto.key != passwd) return Ok("nope");
        GlobalControl.ProcessEmails = dto.ProcessEmails;
        GlobalControl.ProcessFailedEmailsParsing = dto.ProcessFailedEmailsParsing;
        GlobalControl.LogOpenAIEmailParsingObjects = dto.LogOpenAIEmailParsingObjects;
        var res = new ControlDTO
        {
            ProcessEmails = GlobalControl.ProcessEmails,
            ProcessFailedEmailsParsing = GlobalControl.ProcessFailedEmailsParsing,
            LogOpenAIEmailParsingObjects = GlobalControl.LogOpenAIEmailParsingObjects
        };
        return Ok(res);
    }
    [HttpGet("ControlVars/{key}")]
    public async Task<IActionResult> ControlVars(string key)
    {
        if (key != passwd) return Ok("nope");
        var res = new ControlDTO
        {
            ProcessEmails = GlobalControl.ProcessEmails,
            ProcessFailedEmailsParsing = GlobalControl.ProcessFailedEmailsParsing,
            LogOpenAIEmailParsingObjects = GlobalControl.LogOpenAIEmailParsingObjects
        };
        return Ok(res);
    }

    //[HttpDelete("DeleteOurSubs/{key}")]
    //public async Task<IActionResult> DeleteOurSubs(string key)
    //{
    //    if (key != passwd) return Ok("nope");
    //    var emailconn = await appDb.ConnectedEmails.Where(a => a.tenantId == "d0a40b73-985f-48ee-b349-93b8a06c8384").ToListAsync();
    //    _adGraphWrapper.CreateClient("d0a40b73-985f-48ee-b349-93b8a06c8384");
    //    foreach (var item in emailconn)
    //    {
    //        await _adGraphWrapper._graphClient.Subscriptions[item.GraphSubscriptionId.ToString()].DeleteAsync();
    //    }
    //    return Ok();
    //}

    /// <summary>
    /// deletes subscribtion and hangfire renewal job but otherwise leaves db as is
    /// </summary>
    /// <param name="key"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    //[HttpDelete("DeleteSubs/{key}/{email}")]
    //public async Task<IActionResult> DeleteSubs(string key, string email)
    //{
    //    if (key != passwd) return Ok("nope");
    //    var emailconn = await appDb.ConnectedEmails.FirstAsync(e => e.Email == email);
    //    Hangfire.BackgroundJob.Delete(emailconn.SubsRenewalJobId);
    //    _adGraphWrapper.CreateClient(emailconn.tenantId);
    //    await _adGraphWrapper._graphClient.Subscriptions[emailconn.GraphSubscriptionId.ToString()].DeleteAsync();
    //    return Ok();
    //}


    //delet alll avout a solo broker except stripe doesnt touch it
    [HttpDelete("DeleteBroker/{key}/{id}/{agencyId}")]
    public async Task<IActionResult> DeleteBroker(string key, string id, int agencyId)
    {
        if (key != passwd) return Ok("nope");
        await _brokerTagsQService.DeleteSoloBrokerWithoutTouchingStripeAsync(Guid.Parse(id), agencyId);
        return Ok();
    }

    /// <summary>
    /// disables email automation for a broker
    /// </summary>
    /// <param name="key"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    [HttpDelete("DisableAutomation/{key}/{email}")]
    public async Task<IActionResult> DisableAutomation(string key, string email)
    {
        if (key != passwd) return Ok("nope");
        var emailconn = await appDb.ConnectedEmails.FirstAsync(e => e.Email == email);
        _adGraphWrapper.CreateClient(emailconn.tenantId);
        await _adGraphWrapper._graphClient.Subscriptions[emailconn.GraphSubscriptionId.ToString()].DeleteAsync();
        Hangfire.BackgroundJob.Delete(emailconn.SubsRenewalJobId);
        if(emailconn.SyncJobId != null) BackgroundJob.Delete(emailconn.SyncJobId);
        var broker = await appDb.Brokers
            .Include(b => b.RecurrentTasks).FirstAsync(b => b.Id == emailconn.BrokerId);
        foreach (var item in broker.RecurrentTasks)
        {
            if(item is BrokerNotifAnalyzerTask)
            {
                RecurringJob.RemoveIfExists(item.HangfireTaskId);
                broker.RecurrentTasks.Remove(item);
            }       
        }
        await appDb.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// reconnects email automation, and reschedules cleaner if its chris cuz i deleted his taks par erreur
    /// </summary>
    /// <param name="key"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    [HttpGet("reconnectAutomation/{key}/{email}")]
    public async Task<IActionResult> reconnectAutomation(string key, string email)
    {
        if (key != passwd) return Ok("nope");
        var connectedEmail = await appDb.ConnectedEmails.FirstAsync(e => e.Email == email);
        var currDateTime = DateTime.UtcNow;
        //the maxinum subs period = just under 3 days
        DateTimeOffset SubsEnds = currDateTime + new TimeSpan(0, 4230, 0);

        var subs = new Subscription
        {
            ChangeType = "created",
            ClientState = VariousCons.MSFtWebhookSecret,
            ExpirationDateTime = SubsEnds,
            NotificationUrl = _EmailconfigurationSection["MainAPI"] + "/MsftWebhook/Webhook",
            Resource = $"users/{connectedEmail.Email}/mailFolders/inbox/messages"
        };

        _adGraphWrapper.CreateClient(connectedEmail.tenantId);

        var CreatedSubs = await _adGraphWrapper._graphClient.Subscriptions.PostAsync(subs);

        connectedEmail.SubsExpiryDate = (DateTime)(CreatedSubs.ExpirationDateTime?.UtcDateTime);
        connectedEmail.GraphSubscriptionId = Guid.Parse(CreatedSubs.Id);

        var renewalTime = SubsEnds - TimeSpan.FromMinutes(120);
        string RenewalJobId = BackgroundJob.Schedule<EmailProcessor>(s => s.RenewSubscriptionAsync(connectedEmail.Email, CancellationToken.None), renewalTime);
        connectedEmail.SubsRenewalJobId = RenewalJobId;

        var brokerId = connectedEmail.BrokerId;

        var recJobOptions = new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc };
        var HangfireAnalyzerId = brokerId.ToString() + "Analyzer";
        Random rnd = new Random();
        var minute = rnd.Next(0, 59);
        //analyzer every hour except between 2 and 3 montreal time
        RecurringJob.AddOrUpdate<NotifAnalyzer>(HangfireAnalyzerId, a => a.AnalyzeNotifsAsync(brokerId, null, CancellationToken.None), $"{minute} 0-5,7-23 * * *", recJobOptions);
        var recTask = new BrokerNotifAnalyzerTask
        {
            HangfireTaskId = HangfireAnalyzerId,
            BrokerId = brokerId
        };
        appDb.Add(recTask);

        if(connectedEmail.Email == "christopher.g@cpgrealty.ca")
        {
            var HangfireCleanerId = brokerId.ToString() + "Cleaner";
            minute = rnd.Next(1, 20);
            //2:01 to 2:20 AM montreal time CLEANUP
            RecurringJob.AddOrUpdate<ResourceCleaner>(HangfireCleanerId, a => a.CleanBrokerResourcesAsync(brokerId, null, CancellationToken.None), $"{minute} 6 * * *", recJobOptions);
            var recTaskCleaner = new BrokerCleanupTask
            {
                HangfireTaskId = HangfireCleanerId,
                BrokerId = brokerId
            };
            appDb.Add(recTaskCleaner);
        }
        await appDb.SaveChangesAsync();
        return Ok();
    }

    //in case you wanna mnually set up payment info of an account
    //[HttpGet("Chris/{key}/{agencyId}")]
    //public async Task<IActionResult> SetupChris(string key,int agencyId)
    //{
    //    if (key != passwd) return Ok("nope");
    //    var chrisAgency = await appDb.Agencies
    //        .Include(a => a.AgencyBrokers)
    //        .FirstAsync(a => a.Id == agencyId);
    //    chrisAgency.SignupDateTime = new DateTime(2023, 07, 02,19,50,21,DateTimeKind.Utc);
    //    chrisAgency.AdminStripeId = "cus_OBu72ka1sfTELz";
    //    chrisAgency.StripeSubscriptionId = "sub_1NPWJhLJTitiwBgVKKgyn67Z";
    //    chrisAgency.StripeSubscriptionStatus =  Core.Domain.AgencyAggregate.StripeSubscriptionStatus.Active;
    //    chrisAgency.LastCheckoutSessionID = "cs_live_a1JQwhLKv0yZ7iJb5kvuPzXAUhmeHWn9SqbQN1F7PFUBg2xzhiPtXGEGUH";
    //    chrisAgency.NumberOfBrokersInDatabase = 1;
    //    chrisAgency.NumberOfBrokersInSubscription = 1;
    //    chrisAgency.SubscriptionLastValidDate = new DateTime(2023,08,02,19,52,17,DateTimeKind.Utc);
    //    chrisAgency.AgencyBrokers[0].AccountActive = true;
    //    chrisAgency.AgencyBrokers[0].Created = new DateTime(2023,07,02,19,50,21,DateTimeKind.Utc);

    //    return Ok();
    //}
}

