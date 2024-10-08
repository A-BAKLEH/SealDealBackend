﻿using Core.Constants;
using Hangfire;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;
using Web.ApiModels.RequestDTOs.Admin;
using Web.Constants;
using Web.ControllerServices.QuickServices;
using Web.Outbox.Config;
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
    private readonly MyGmailQService _myGmail;
    public TestProdController(IConfiguration configuration,
        AppDbContext dbContext,
        ILogger<TestProdController> logger,
        IWebHostEnvironment webHostEnv,
        ADGraphWrapper aDGraphWrapper,
        AppDbContext context,
        BrokerQService brokerQService,
        EmailProcessor _emailProcessor,
        MyGmailQService _myGmail)
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

    [HttpGet("fixLeadStatusesProd/{key}")]
    //public async Task<IActionResult> FixLeadStatusProd(string key)
    //{
    //    if (key != passwd) return Ok("nope");
    //    var appEvent = await appDb.AppEvents.FirstAsync(e => e.Id == 89);
    //    appEvent.Props["OldLeadStatus"] = "Hot";
    //    appDb.Entry(appEvent).State = EntityState.Modified;
    //    appDb.Entry(appEvent).Property(e => e.Props).IsModified = true;
    //    await appDb.SaveChangesAsync();
    //    await appDb.Database.ExecuteSqlRawAsync
    //        (
    //          "UPDATE \"Leads\" SET \"LeadStatus\"='Hot' Where \"LeadStatus\"='New';"
    //        );
    //    return Ok();
    //}

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
        GlobalControl.LogAllEmailsLengthsOpenAi = dto.LogAllEmailsLengthsOpenAi;
        GlobalControl.LogAnalyzerSteps = dto.LogAnalyzerSteps;

        var res = new ControlDTO
        {
            ProcessEmails = GlobalControl.ProcessEmails,
            ProcessFailedEmailsParsing = GlobalControl.ProcessFailedEmailsParsing,
            LogOpenAIEmailParsingObjects = GlobalControl.LogOpenAIEmailParsingObjects,
            LogAllEmailsLengthsOpenAi = GlobalControl.LogAllEmailsLengthsOpenAi,
            LogAnalyzerSteps = GlobalControl.LogAnalyzerSteps
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
            LogOpenAIEmailParsingObjects = GlobalControl.LogOpenAIEmailParsingObjects,
            LogAllEmailsLengthsOpenAi = GlobalControl.LogAllEmailsLengthsOpenAi,
            LogAnalyzerSteps = GlobalControl.LogAnalyzerSteps
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
    /// //notif analyzer should be able to handle when no connected emails exist
        ////JUST MAKE SURE NO ACTION PLANS RUN
    /// </summary>
    /// <param name="key"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    [HttpDelete("DisableAutomationMSFT/{key}/{email}")]
    public async Task<IActionResult> DisableAutomationMsft(string key, string email)
    {
        if (key != passwd) return Ok("nope");
        var emailconn = await appDb.ConnectedEmails.FirstAsync(e => e.Email == email && e.isMSFT);
        _adGraphWrapper.CreateClient(emailconn.tenantId);
        await _adGraphWrapper._graphClient.Subscriptions[emailconn.GraphSubscriptionId.ToString()].DeleteAsync();
        if(emailconn.SubsRenewalJobId != null) BackgroundJob.Delete(emailconn.SubsRenewalJobId);
        if (emailconn.SyncJobId != null) BackgroundJob.Delete(emailconn.SyncJobId);

        await appDb.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// //notif analyzer should be able to handle when no connected emails exist
        ////JUST MAKE SURE NO ACTION PLANS RUN
    /// </summary>
    /// <param name="key"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    [HttpDelete("DisableAutomationGmail/{key}/{email}")]
    public async Task<IActionResult> DisableAutomationGmail(string key, string email)
    {
        if (key != passwd) return Ok("nope");
        var connEmail = await appDb.ConnectedEmails.FirstAsync(e => e.Email == email && !e.isMSFT);
        await _myGmail.CallUnwatch(connEmail.Email, connEmail.BrokerId);
        //var jobIdRefresh = connEmail.TokenRefreshJobId; KEEP REFRESHING ACCESS TOKEN SO U CAN DO STUFF FROM FRONTEND
        //Hangfire.BackgroundJob.Delete(jobIdRefresh);
        if(connEmail.SyncJobId != null) BackgroundJob.Delete(connEmail.SyncJobId);
        if(connEmail.SubsRenewalJobId != null) RecurringJob.RemoveIfExists(connEmail.SubsRenewalJobId);
        return Ok();
    }

    /// <summary>
    /// reconnects email automation, and reschedules cleaner if its chris cuz i deleted his taks par erreur
    /// </summary>
    /// <param name="key"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    [HttpGet("reconnectAutomationMsft/{key}/{email}")]
    public async Task<IActionResult> reconnectAutomationMsft(string key, string email)
    {
        if (key != passwd) return Ok("nope");
        var connectedEmail = await appDb.ConnectedEmails.FirstAsync(e => e.Email == email && e.isMSFT);
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

        //notif analyzer wasnt deleted
        await appDb.SaveChangesAsync();
        return Ok();
    }


    /// <summary>
    /// reconnects email automation, and reschedules cleaner if its chris cuz i deleted his taks par erreur
    /// </summary>
    /// <param name="key"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    [HttpGet("reconnectAutomationGmail/{key}/{email}")]
    public async Task<IActionResult> reconnectAutomationGmail(string key, string email)
    {
        if (key != passwd) return Ok("nope");
        var connectedEmail = await appDb.ConnectedEmails.FirstAsync(e => e.Email == email && e.isMSFT);

        await _myGmail.CallWatch(email, connectedEmail.BrokerId);

        //hangfire recurrrent job that calls watch once every day
        string WebhooksubscriptionRenewalJobId = email + "Watch";
        var recJobOptions = new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc };
        var TimeNow = DateTime.UtcNow;
        var hour = TimeNow.Hour;
        var minute = TimeNow.Minute;
        if (hour == 6 || hour == 7) hour = 1;
        RecurringJob.AddOrUpdate<MyGmailQService>(WebhooksubscriptionRenewalJobId,
            a => a.CallWatch(email, connectedEmail.BrokerId), $"{minute} {hour} * * *", recJobOptions);
        connectedEmail.SubsRenewalJobId = WebhooksubscriptionRenewalJobId;
        await appDb.SaveChangesAsync();
        return Ok();
    }


    /// <summary>
    /// removes graph api subs except the one in the database
    /// </summary>
    /// <param name="key"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    [HttpDelete("CleanSubs/{key}/{email}")]
    public async Task<IActionResult> CleanSubs(string key, string email)
    {
        if (key != passwd) return Ok("nope");
        var emailconn = await appDb.ConnectedEmails.FirstAsync(e => e.Email == email);
        _adGraphWrapper.CreateClient(emailconn.tenantId);
        var Subs1 = await _adGraphWrapper._graphClient.Subscriptions.GetAsync();
        var subs = Subs1.Value;
        foreach (var item in subs)
        {
            if (emailconn.GraphSubscriptionId.ToString() != item.Id)
            {
                await _adGraphWrapper._graphClient.Subscriptions[item.Id].DeleteAsync();
                _logger.LogInformation($"deleting subs {item.Id}");
            }
        }
        return Ok();
    }
    //[HttpGet("AnalyzerTime/{key}")]
    //public async Task<IActionResult> AnalyzerTime(string key)
    //{
    //    if (key != passwd) return Ok("nope");
    //    var job1 = "1c96b780-953a-463b-a17f-71400166309cAnalyzer";
    //    var job2 = "35a8e010-b2ad-4d75-a93c-e6595b35f0dfAnalyzer";

    //    var recJobOptions = new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc };

    //    Random rnd = new Random();
    //    var minute = rnd.Next(0, 59);
    //    var id1 = Guid.Parse("1c96b780-953a-463b-a17f-71400166309c");
    //    RecurringJob.AddOrUpdate<NotifAnalyzer>(job1, a => a.AnalyzeNotifsAsync(id1, null, CancellationToken.None), $"{minute} 0-5,8-23 * * *", recJobOptions);

    //    minute = rnd.Next(0, 59);
    //    var id2 = Guid.Parse("35a8e010-b2ad-4d75-a93c-e6595b35f0df");
    //    RecurringJob.AddOrUpdate<NotifAnalyzer>(job2, a => a.AnalyzeNotifsAsync(id2, null, CancellationToken.None), $"{minute} 0-5,8-23 * * *", recJobOptions);
    //    return Ok();
    //}

    //[HttpGet("ScheduleOutboxDict/{key}")]
    //public async Task<IActionResult> ScheduleOutboxDict(string key)
    //{
    //    if (key != passwd) return Ok("nope");
    //    var exists = await appDb.OutboxDictsTasks.AnyAsync();

    //    if (exists) return Ok("already exists");
    //    var HangfireoutboxTaskId = Guid.NewGuid().ToString();
    //    RecurringJob.AddOrUpdate<OutboxCleaner>(HangfireoutboxTaskId, a => a.CleanOutbox(null,CancellationToken.None), "*/6 * * * *");
    //    var outboxTask = new OutboxDictsTask { HangfireTaskId = HangfireoutboxTaskId };
    //    appDbContext.Add(outboxTask);
    //    await appDbContext.SaveChangesAsync();
    //    return Ok();
    //}

    [HttpGet("CountOutboxDict/{key}")]
    public IActionResult geteOutboxDictCount(string key)
    {
        if (key != passwd) return Ok("nope");
        var c = OutboxMemCache.SchedulingErrorDict.Count;
        return Ok(c);
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

