using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.ApiModels.RequestDTOs.Admin;
using Web.Constants;
using Web.ControllerServices.QuickServices;

namespace Web.Api.TestingAPI;

[Route("api/[controller]")]
[ApiController]
public class TestProdController : ControllerBase
{
    private IConfigurationSection section;
    private readonly IWebHostEnvironment _webHostEnv;
    private readonly AppDbContext appDbContext;
    private readonly ILogger<TestProdController> _logger;
    private string passwd = "helloHabibi69";
    private readonly ADGraphWrapper _adGraphWrapper;
    private readonly AppDbContext appDb;
    private readonly BrokerQService _brokerTagsQService;
    public TestProdController(IConfiguration configuration,
        AppDbContext dbContext,
        ILogger<TestProdController> logger,
        IWebHostEnvironment webHostEnv,
        ADGraphWrapper aDGraphWrapper,
        AppDbContext context,
        BrokerQService brokerQService)
    {
        section = configuration.GetSection("Hangfire");
        _webHostEnv = webHostEnv;
        appDbContext = dbContext;
        _logger = logger;
        _adGraphWrapper = aDGraphWrapper;
        appDb = context;
        _brokerTagsQService = brokerQService;
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

    [HttpDelete("DeleteOurSubs/{key}")]
    public async Task<IActionResult> DeleteOurSubs(string key)
    {
        if (key != passwd) return Ok("nope");
        var emailconn = await appDb.ConnectedEmails.Where(a => a.tenantId == "d0a40b73-985f-48ee-b349-93b8a06c8384").ToListAsync();
        _adGraphWrapper.CreateClient("d0a40b73-985f-48ee-b349-93b8a06c8384");
        foreach (var item in emailconn)
        {
            await _adGraphWrapper._graphClient.Subscriptions[item.GraphSubscriptionId.ToString()].DeleteAsync();
        }
        return Ok();
    }

    [HttpDelete("DeleteSubs/{key}/{email}")]
    public async Task<IActionResult> DeleteSubs(string key, string email)
    {
        if (key != passwd) return Ok("nope");
        var emailconn = await appDb.ConnectedEmails.FirstAsync(e => e.Email == email);
        _adGraphWrapper.CreateClient(emailconn.tenantId);
        await _adGraphWrapper._graphClient.Subscriptions[emailconn.GraphSubscriptionId.ToString()].DeleteAsync();
        return Ok();
    }

    [HttpDelete("DeleteBroker/{key}/{id}/{agencyId}")]
    public async Task<IActionResult> DeleteBroker(string key, string id,int agencyId )
    {
        if (key != passwd) return Ok("nope");
        await _brokerTagsQService.DeleteSoloBrokerWithoutTouchingStripeAsync(Guid.Parse(id),agencyId);
        return Ok();
    }

    [HttpGet("Chris/{key}/{agencyId}")]
    public async Task<IActionResult> SetupChris(string key,int agencyId)
    {
        if (key != passwd) return Ok("nope");
        var chrisAgency = await appDb.Agencies
            .Include(a => a.AgencyBrokers)
            .FirstAsync(a => a.Id == agencyId);
        chrisAgency.SignupDateTime = new DateTime(2023, 07, 02,19,50,21,DateTimeKind.Utc);
        chrisAgency.AdminStripeId = "cus_OBu72ka1sfTELz";
        chrisAgency.StripeSubscriptionId = "sub_1NPWJhLJTitiwBgVKKgyn67Z";
        chrisAgency.StripeSubscriptionStatus =  Core.Domain.AgencyAggregate.StripeSubscriptionStatus.Active;
        chrisAgency.LastCheckoutSessionID = "cs_live_a1JQwhLKv0yZ7iJb5kvuPzXAUhmeHWn9SqbQN1F7PFUBg2xzhiPtXGEGUH";
        chrisAgency.NumberOfBrokersInDatabase = 1;
        chrisAgency.NumberOfBrokersInSubscription = 1;
        chrisAgency.SubscriptionLastValidDate = new DateTime(2023,08,02,19,52,17,DateTimeKind.Utc);
        chrisAgency.AgencyBrokers[0].AccountActive = true;
        chrisAgency.AgencyBrokers[0].Created = new DateTime(2023,07,02,19,50,21,DateTimeKind.Utc);

        return Ok();
    }
}

