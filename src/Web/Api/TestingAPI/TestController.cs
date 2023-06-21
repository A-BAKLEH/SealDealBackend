
using Core.Domain.AgencyAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.BrokerAggregate.Templates;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.ExternalServiceInterfaces;
using Hangfire;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TimeZoneConverter;
using Web.ApiModels.RequestDTOs;
using Web.Config;
using Web.Constants;
using Web.ControllerServices.QuickServices;
using Web.ControllerServices.StaticMethods;
using Web.Processing.Analyzer;
using Web.Processing.EmailAutomation;
using Web.RealTimeNotifs;

namespace Web.Api.TestingAPI;

[DevOnly]
[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{

    private readonly IMediator _mediator;
    private readonly ILogger<TestController> _logger;
    private readonly IB2CGraphService _graphService;
    private readonly ADGraphWrapper _adGraphWrapper;

    private readonly AppDbContext _appDbContext;
    private readonly TemplatesQService _templatesQService;
    //private readonly IDistributedCache _distributedCache;
    private readonly BrokerQService _brokerTagsQService;
    private readonly ActionPQService _actionPQService;
    private readonly IHubContext<NotifsHub> hub;

    public TestController(IMediator mediator, ILogger<TestController> logger, ADGraphWrapper aDGraph,
       AppDbContext appDbContext, IB2CGraphService msGraphService, TemplatesQService templatesQService,
       //IDistributedCache _distributedCache,
       BrokerQService brokerTagsQService,
       ActionPQService actionPQService,
       IHubContext<NotifsHub> hubContext)
    {
        _mediator = mediator;
        _logger = logger;
        //this._distributedCache = _distributedCache;
        _appDbContext = appDbContext;
        _templatesQService = templatesQService;
        _brokerTagsQService = brokerTagsQService;
        _adGraphWrapper = aDGraph;
        _actionPQService = actionPQService;
        hub = hubContext;
    }


    [HttpGet("testHub")]
    public async Task<IActionResult> testHub()
    {
        //var id = Guid.Parse("576df38e-acdb-4ce5-9323-fb3529245e87");
        var id = Guid.NewGuid();
        try
        {
            await hub.Clients.User(id.ToString()).SendAsync("ReceiveMessage", "hello from server lmao");
        }
        catch (Exception ex)
        {

        }

        return Ok();
    }
    [HttpGet("testtemplateAbstract")]
    public async Task<IActionResult> testtemplateAbstract()
    {
        var template = await _appDbContext.Templates.FirstAsync(t => t.Id == 6);

        //if (dto.text != null && dto.text != template.templateText) template.templateText = dto.text;
        //if (dto.TemplateName != null) template.Title = dto.TemplateName;
        //template.EmailTemplateSubject = dto.subject;
        if (template is EmailTemplate)
        {
            var template2 = (EmailTemplate)template;
            template2.EmailTemplateSubject = "changed subject";
        }
        template.Modified = DateTime.UtcNow;
        await _appDbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("testtranslation")]
    public async Task<IActionResult> testtranslation()
    {
        return Ok();
    }


    [HttpGet("startmailprocessor")]
    public async Task<IActionResult> startmailprocessor()
    {
        var SubsId = Guid.Parse("149CFB0A-0B0D-423A-B5E9-0F99E143EF07");
        if (StaticEmailConcurrencyHandler.EmailParsingdict.TryAdd(SubsId, true))
        {
            var connEmail = await _appDbContext.ConnectedEmails.FirstAsync(e => e.GraphSubscriptionId == SubsId);
            //'lock' obtained by putting subsID as key in dictionary
            string jobId = "";
            try
            {
                jobId = BackgroundJob.Schedule<EmailProcessor>(e => e.SyncEmailAsync(connEmail.Email, null), GlobalControl.EmailStartSyncingDelay);
                _logger.LogInformation("{place} scheduled email parsing with", "ScheduleEmailParseing", "ScheduleEmailParseing");
            }
            catch (Exception ex)
            {
                _logger.LogError("{place} error scheduling email parsing with error {error}", "ScheduleEmailParseing", ex.Message);
                StaticEmailConcurrencyHandler.EmailParsingdict.TryRemove(SubsId, out var s);
                return Ok();
            }
            try
            {
                connEmail.SyncScheduled = true;
                connEmail.SyncJobId = jobId;
                await _appDbContext.SaveChangesAsync();
                _logger.LogInformation("{place} scheduled email parsing with", "savingEmailParseing");
            }
            catch (Exception ex)
            {
                _logger.LogError("{place} error saving db after scheduling email parsing with error {error}", "ScheduleEmailParseing", ex.Message);

                BackgroundJob.Delete(jobId);
                StaticEmailConcurrencyHandler.EmailParsingdict.TryRemove(SubsId, out var s);
            }
        }
        return Ok();
    }
    [HttpGet("testaddress")]
    public async Task<IActionResult> testaddress()
    {

        var addres = "chalet gatineau";
        var listings = await _appDbContext.Listings
                .Where(x => x.AgencyId == 56 && EF.Functions.Like(x.FormattedStreetAddress, $"{addres}%"))
                .AsNoTracking()
                .ToListAsync();
        return Ok();
    }
    [HttpGet("testpostgresExecuteUpdate")]
    public async Task<IActionResult> testgresExecuteUpdate()
    {
        var task1 = await _appDbContext.AppEvents
            .Where(e => e.LeadId == 2)
                .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.DeleteAfterProcessing, false));

        return Ok();
    }

    [HttpGet("StartAnalyzer")]
    public async Task<IActionResult> StartAnalyzer()
    {
        var found = await _appDbContext.Brokers.FirstAsync(b => b.isAdmin);
        Hangfire.BackgroundJob.Enqueue<NotifAnalyzer>(x => x.AnalyzeNotifsAsync(found.Id,null));
        return Ok();
    }


    [HttpGet("addTestNotifs")]
    public async Task<IActionResult> addTestNOtifs()
    {
        await _appDbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("test-params")]
    public async Task<IActionResult> test_params()
    {
        var Email = "bashar.eskandar@sealdeal.ca";
        var guidd = Guid.Parse("EA14ECF1-FCDA-43C4-9325-197A953D58FA").ToString();
        _appDbContext.Database.ExecuteSqlRaw($"UPDATE [dbo].[ConnectedEmails] SET OpenAITokensUsed = OpenAITokensUsed + 1" +
            $" WHERE Email = '{Email}' AND BrokerId = '{guidd}';");
        return Ok();
    }
    [HttpGet("createtestData")]
    public async Task<IActionResult> createTestData()
    {

        var listing = new Listing
        {
            Address = new Address
            {
                City = "laval",
                StreetAddress = "611 rue dsi",
                Country = "tanaka",
                PostalCode = "h7psg5",
                ProvinceState = "qc"
            },
            AgencyId = 40,
            AssignedBrokersCount = 1,
            BrokersAssigned = new List<BrokerListingAssignment>
            {
              new BrokerListingAssignment
              {
                assignmentDate= DateTime.UtcNow,
                BrokerId = Guid.Parse("B01427D3-E653-48B5-B2F2-DED2B6C895F7"),
              }
            }
        };
        var lead = new Lead
        {
            AgencyId = 40,
            LeadFirstName = "asad",
            LeadLastName = "abdullah",
            EntryDate = DateTime.UtcNow,
            source = LeadSource.manual,
            leadType = LeadType.Buyer,
            BrokerId = Guid.Parse("B01427D3-E653-48B5-B2F2-DED2B6C895F7"),
            Listing = listing,
            Budget = 1000,
            LeadStatus = LeadStatus.New,
            Note = new Note { NotesText = "wlasdfjasdogihoig" },
            Tags = new List<Tag> { new Tag { BrokerId = Guid.Parse("B01427D3-E653-48B5-B2F2-DED2B6C895F7"), TagName = "wlakTagTaggedlmaoNerd" } },
            AppEvents = new List<AppEvent>
            {
              new AppEvent
              {
              DeleteAfterProcessing = false,
              IsActionPlanResult = false,
              BrokerId = Guid.Parse("B01427D3-E653-48B5-B2F2-DED2B6C895F7"),
              EventTimeStamp= DateTime.UtcNow,
              EventType = EventType.LeadStatusChange,
              NotifyBroker = false,
              ProcessingStatus = ProcessingStatus.Done,
              ReadByBroker= true
              }
            }
        };
        _appDbContext.Leads.Add(lead);
        await _appDbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("testGroupBy")]
    public async Task<IActionResult> testGroupBy()
    {
        return Ok();
    }

    [HttpGet("create-data")]
    public async Task<IActionResult> createData()
    {

        var broker = new Broker
        {
            AccountActive = true,
            isAdmin = true,
            Created = DateTime.UtcNow,
            FirstName = "test",
            LastName = "test",
            LoginEmail = "test",
        };
        var agency = new Agency
        {
            AgencyName = "Test",
            NumberOfBrokersInDatabase = 1,
            NumberOfBrokersInSubscription = 1,
            SignupDateTime = DateTime.UtcNow,
            StripeSubscriptionStatus = StripeSubscriptionStatus.Active,
            AgencyBrokers = new List<Broker> { broker }
        };
        _appDbContext.Agencies.Add(agency);
        _appDbContext.SaveChanges();

        return Ok();
    }

    [HttpPost("test-timeZone")]
    public async Task<IActionResult> atimeZoneTest([FromBody] CreateListingRequestDTO dto)
    {
        var timeZoneInfoTZ = TZConvert.GetTimeZoneInfo("Eastern Standard Time");
        var convertedTime = MyTimeZoneConverter.ConvertToUTC(timeZoneInfoTZ, dto.DateOfListing);

        var timeZoneId = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var converted = MyTimeZoneConverter.ConvertToUTC(timeZoneId, dto.DateOfListing);
        return Ok();
    }
}
