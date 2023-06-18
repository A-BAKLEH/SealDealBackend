﻿
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TimeZoneConverter;
using Web.ApiModels.RequestDTOs;
using Web.Constants;
using Web.ControllerServices.QuickServices;
using Web.ControllerServices.StaticMethods;
using Web.HTTPClients;
using Web.Processing.Analyzer;
using Web.Processing.EmailAutomation;

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
    //private readonly IDistributedCache _distributedCache;
    private readonly BrokerQService _brokerTagsQService;
    private readonly ActionPQService _actionPQService;

    public TestController(IMediator mediator, ILogger<TestController> logger, ADGraphWrapper aDGraph,
       AppDbContext appDbContext, IB2CGraphService msGraphService, TemplatesQService templatesQService,
       //IDistributedCache _distributedCache,
       BrokerQService brokerTagsQService,
       ActionPQService actionPQService)
    {
        _mediator = mediator;
        _logger = logger;
        //this._distributedCache = _distributedCache;
        _appDbContext = appDbContext;
        _templatesQService = templatesQService;
        _brokerTagsQService = brokerTagsQService;
        _adGraphWrapper = aDGraph;
        _actionPQService = actionPQService;
    }

    [HttpGet("testtranslation")]
    public async Task<IActionResult> testtranslation()
    {
        try
        {
            var template = await _appDbContext.Templates.FirstAsync(t => t.Id == 4);
            var t = (EmailTemplate)template;

            var TemplateText = "hello %firstname% %lastname%,\\n I received your email and I look forward to working" +
                "with you.\\n You can contact me whenever you are free at 514 512 9956.\\n Have a nice day!";
            string prompt = APIConstants.TranslateTemplatePrompt + TemplateText;
            HttpClient _httpClient = new();
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/chat/completions");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "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");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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

            HttpResponseMessage response = await _httpClient.PostAsync("", content: jsonContent);

            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var rawResponse = JsonSerializer.Deserialize<GPT35RawResponse>(jsonResponse);
            //var GPTCompletionJSON = rawResponse.choices[0].message.content.Replace("\n", "");
            var GPTCompletionJSON = rawResponse.choices[0].message.content.Replace("\n", "");
            var templateTranslated = JsonSerializer.Deserialize<TemplateTranslationContent>(GPTCompletionJSON);
        }
        catch (Exception ex)
        {
            var exx = ex;
            _logger.LogError("{tag} wassup fool with error {error}", "test");
        }

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
