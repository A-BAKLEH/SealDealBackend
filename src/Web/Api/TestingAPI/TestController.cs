
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

    [HttpGet("testdictRemove")]
    public async Task<IActionResult> testdictRemove()
    {
        //var appEvent = new AppEvent
        //{
        //    BrokerId = Guid.Parse("EA14ECF1-FCDA-43C4-9325-197A953D58FA"),
        //    LeadId = null,
        //};
        //appEvent.Props["lol"] = "lol";
        var found = await _appDbContext.AppEvents.FindAsync(25);

        //TECH
        found.Props.Remove("lol");
        _appDbContext.Entry(found).Property(f => f.Props).IsModified = true;
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
