
using Core.Domain.AgencyAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.ExternalServiceInterfaces;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using TimeZoneConverter;
using Web.ApiModels.RequestDTOs;
using Web.ControllerServices.QuickServices;
using Web.ControllerServices.StaticMethods;
using Web.Processing.Analyzer;

namespace Web.Api.TestingAPI;


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
    private readonly IDistributedCache _distributedCache;
    private readonly BrokerQService _brokerTagsQService;
    private readonly AgencyQService _agencyQService;
    private readonly ActionPQService _actionPQService;

    public TestController(IMediator mediator, ILogger<TestController> logger, ADGraphWrapper aDGraph,
       AppDbContext appDbContext, IB2CGraphService msGraphService, TemplatesQService templatesQService,
       IDistributedCache _distributedCache, BrokerQService brokerTagsQService,
       ActionPQService actionPQService, AgencyQService agencyQService)
    {
        _mediator = mediator;
        _logger = logger;
        this._distributedCache = _distributedCache;
        _appDbContext = appDbContext;
        _templatesQService = templatesQService;
        _brokerTagsQService = brokerTagsQService;
        _agencyQService = agencyQService;
        _adGraphWrapper = aDGraph;
        _actionPQService = actionPQService;
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

    [HttpGet("StartAnalyzer")]
    public async Task<IActionResult> StartAnalyzer()
    {
        var found = await _appDbContext.Brokers.FirstAsync(b => b.isAdmin);
        Hangfire.BackgroundJob.Enqueue<NotifAnalyzer>(x => x.AnalyzeNotifsAsync(found.Id));
        return Ok();
    }


    [HttpGet("addTestNotifs")]
    public async Task<IActionResult> addTestNOtifs()
    {
        //admin  "EA14ECF1-FCDA-43C4-9325-197A953D58FA"
        //broker "08BC58DE-1F82-4BED-B7AB-D33998CAD81A"


        await _appDbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("test-params")]
    public async Task<IActionResult> test_params()
    {
        //var notif = new ConnectedEmail {Email = "bashar.eskandar@sealdeal.ca",BrokerId = Guid.Parse("EA14ECF1-FCDA-43C4-9325-197A953D58FA"), AssignLeadsAuto = true};
        //_appDbContext.ConnectedEmails.Update(notif);
        //_appDbContext.SaveChanges();
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
            EntryDate = DateTimeOffset.UtcNow,
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
              EventTimeStamp= DateTimeOffset.UtcNow,
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
        //var id = Guid.Parse("EA14ECF1-FCDA-43C4-9325-197A953D58FA");
        //var EmailEventsTask = await _appDbContext.EmailEvents
        //    .Where(e => e.BrokerId == id && !e.Seen && e.LeadId != null)
        //    .Select(e => new { e.Id, e.LeadId, e.BrokerEmail, e.Seen, e.TimeReceived })
        //    .GroupBy(e => e.LeadId)
        //    .Select(g => new {g.Key,  })
        //    .AsNoTracking()
        //    .ToListAsync();
        // var lol = id;
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
