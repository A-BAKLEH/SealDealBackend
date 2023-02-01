
using Core.Domain.AgencyAggregate;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.DTOs.ProcessingDTOs;
using Core.ExternalServiceInterfaces;
using Infrastructure.Data;
using SharedKernel.Exceptions;
using Web.ApiModels.RequestDTOs;
using Web.ControllerServices.QuickServices;
using Web.MediatrRequests.NotifsRequests;
using Web.Cache.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Core.Constants;
using Infrastructure.ExternalServices;
using Microsoft.Graph;
using TimeZoneConverter;
using Humanizer;
using Web.ControllerServices.StaticMethods;
using Core.Domain.BrokerAggregate;
using Infrastructure.Dispatching;
using SharedKernel.DomainNotifications;
using Web.Outbox.Config;
using Web.Outbox;
using Infrastructure.Migrations;
using Core.Domain.ActionPlanAggregate;

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

  public TestController(IMediator mediator, ILogger<TestController> logger, ADGraphWrapper aDGraph,
     AppDbContext appDbContext, IB2CGraphService msGraphService, TemplatesQService templatesQService,
     IDistributedCache _distributedCache, BrokerQService brokerTagsQService, AgencyQService agencyQService)
  {
    _mediator = mediator;
    _logger = logger;
    this._distributedCache = _distributedCache;
    _appDbContext = appDbContext;
    _templatesQService = templatesQService;
    _brokerTagsQService= brokerTagsQService;
    _agencyQService= agencyQService;
    _adGraphWrapper = aDGraph;
  }

  [HttpGet("test-json")]
  public async Task<IActionResult> testJSON()
  {
    _appDbContext.TestEntity1.Add(new Core.Domain.TestAggregate.TestEntity1 { testJSON = new Core.Domain.TestAggregate.TestJSON
    {
      one = new Core.Domain.TestAggregate.Test1Props { prop1 = "lol"}
    } });
    _appDbContext.TestEntity2.Add(new Core.Domain.TestAggregate.TestEntity2
    {
      testJSON = new Core.Domain.TestAggregate.TestJSON
      {
        two = new Core.Domain.TestAggregate.Test2Props { prop_2_2 = "lol2"}
      }
    });
    _appDbContext.SaveChanges();
    return Ok();
  }
  [HttpPost("test-LEadJSON")]
  public async Task<IActionResult> cacheTest()
  {

    //var listing = new Listing { AgencyId = 1, AssignedBrokersCount = 0,
    //  DateOfListing = DateTime.UtcNow,
    //  Price = 1000,
    //  Status = ListingStatus.Listed,
    //  Address = new Address {StreetAddress = "611 rue lol",
    //    City = "laval",
    //    Country = "ca",
    //    PostalCode = "234234",
    //  ProvinceState = "quebec"}
    //};
    //_appDbContext.Listings.Add(listing);
    //_appDbContext.SaveChanges();
    //var listing = _appDbContext.Listings.FirstOrDefault(a => a.Id == 1);

    //var listings = _appDbContext.Listings
    //  .OrderByDescending(l => l.DateOfListing)
    //  .Where(l => l.AgencyId == 1)
    //  .Select( l => new AgencyListingDTO
    //  {
    //    Address = l.Address
    //  }).AsNoTracking().ToList();
    //var listing = _appDbContext.Listings.Where(l => l.Id == 1)
    //  .Include(l => l.BrokersAssigned)
    //  .FirstOrDefault();
    //BrokerListingAssignment brokerlisting = new() { assignmentDate = DateTime.UtcNow, BrokerId = Guid.Parse("00000000-0000-0000-0000-000000000000") };
    //listing.BrokersAssigned = new List<BrokerListingAssignment> { brokerlisting };
    //_appDbContext.SaveChanges();

    var listings = await _appDbContext.BrokerListingAssignments
      .Where(b => b.BrokerId == Guid.Parse("00000000-0000-0000-0000-000000000000"))
      .OrderByDescending(a => a.assignmentDate)
      .Select(l => new BrokerListingDTO
      {
        Address = l.Listing.Address,
        DateOfListing = l.Listing.DateOfListing.UtcDateTime,
        ListingURL = l.Listing.URL,
        Price = l.Listing.Price,
        Status = l.Listing.Status.ToString(),
        DateAssignedToMe = l.assignmentDate,
        AssignedBrokersCount = l.Listing.BrokersAssigned.Count
      }).AsNoTracking().ToListAsync();



    return Ok();
  }

  [HttpGet("test-exceptions")]
  public async Task<IActionResult> exceptionTest()
  {
    var exc = new CustomBadRequestException("details lolol", "title lol");
    //exc.Errors["leadname"] = "no lead name";
    //exc.Errors["phone"] = "bad format";
    exc.ErrorsJSON = new Agency { Id = 1};
    throw exc;
    /*List<Agency> lis = new();
    lis.Add(new Agency {Id = 1 });*/
    //return BadRequest(lis);
  }

  [HttpGet("test-wlak")]
  public async Task<IActionResult> wlak()
  {
    int ActionPlanId = 1;
    var apProjection = _appDbContext.ActionPlans.
      Select(ap => new
      {
        ap.Id,
        ap.StopPlanOnInteraction,
        ap.FirstActionDelay,
        ap.NotifsToListenTo,
        firstAction = ap.Actions.First(a => a.Id == ActionPlanId)
      })
      .First(app => app.Id == ActionPlanId);

    return Ok();

  }
  [HttpGet("test-timestamp")]
  public async Task<IActionResult> testTimestamp()
  {
    var timespan = TimeSpan.Zero;
    var days = 1;
    var hours = 1;
    var minutes = 2;
    timespan += TimeSpan.FromDays(days);
    timespan += TimeSpan.FromHours(hours);
    timespan += TimeSpan.FromMinutes(minutes);

    return Ok();

  }

  [HttpGet("test-jsonsss")]
  public async Task<IActionResult> testjsonnn()
  {
    var lead = new Lead {};
    lead.SourceDetails["wtf"] = "lol";
    return Ok(lead);
  }

  [HttpGet("create-data")]
  public async Task<IActionResult> createData()
  {

    var broker = new Broker
    {
      AccountActive= true,
      isAdmin= true,
      Created = DateTime.UtcNow,
      FirstName = "test",
      LastName = "test",
      LoginEmail= "test",
    };
    var agency = new Agency
    { 
      AgencyName= "Test",
      NumberOfBrokersInDatabase= 1,
      NumberOfBrokersInSubscription= 1,
      SignupDateTime= DateTime.UtcNow,
      StripeSubscriptionStatus = StripeSubscriptionStatus.Active,
      AgencyBrokers= new List<Broker> { broker}
    };
    _appDbContext.Agencies.Add(agency);
    _appDbContext.SaveChanges();

    return Ok();
  }


  [HttpGet("test-new-notifsSytem")]
  public async Task<IActionResult> NewNotifs()
  {

    var notif = new Notification
    {
      APHandlingStatus = APHandlingStatus.Scheduled,
      BrokerId = Guid.Parse("00000000-0000-0000-0000-000000000000"),
      EventTimeStamp= DateTime.UtcNow,
      NotifType = NotifType.BrokerCreated,
      NotifyBroker = false,
      ReadByBroker = false,
    };
    notif.NotifProps.Add("lolkey", "lolvalue");
    _appDbContext.Notifications.Add(notif);
    _appDbContext.SaveChanges();
    var brokerCreated = new BrokerCreated {NotifId = notif.Id };
    var id = Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(brokerCreated));

    return Ok();
  }

  [HttpGet("test-modifyJSON/{id}")]
  public async Task<IActionResult> testwNotifs(int id)
  {

    var notif = _appDbContext.Notifications.FirstOrDefault(x => x.Id == id);
    notif.NotifProps["lolkey"] = "changedValue";

    _appDbContext.Notifications.Update(notif);
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

    


    /* var timeZoneInfo = TZConvert.GetTimeZoneInfo("America/Toronto");

     var id = timeZoneInfo.Id;
     var second = TimeZoneInfo.FindSystemTimeZoneById(id);*/
    return Ok();
  }

  [HttpGet("test-GetListings")]
  public async Task<IActionResult> test_GetListings()
  {
    //var agency = _appDbContext.Agencies.Include(a => a.AgencyBrokers).ThenInclude(b => b.ActionPlans).FirstOrDefault(a => a.Id == 1);
    //var emailtemp = new EmailTemplate
    //{ EmailTemplateSubject = "wlak", EmailTemplateText = "salut habibi qu'est ce pasee" };
    //agency.AgencyBrokers[0].EmailTemplates = new List<EmailTemplate> { emailtemp };
    //var act1 = new SendEmailAction
    //{
    //  ActionLevel = 1,
    //  NextActionDelay = "1:0:0",
    //};
    //act1.ActionProperties[SendEmailAction.EmailTemplateIdKey] = "1";

    //var actionplan = new ActionPlan
    //{
    //  ActionsCount = 1,
    //  AssignToLead = true,
    //  isActive = true,
    //  StopPlanOnInteraction = true,
    //  Title = "first plan",
    //  NotifsToListenTo = NotifType.EmailReceived | NotifType.SmsReceived | NotifType.CallReceived,
    //  Triggers = NotifType.LeadAssigned,
    //  Actions = new List<ActionBase>
    //  {
    //    act1
    //  }
    //};
    //agency.AgencyBrokers[0].ActionPlans.Add(actionplan);
    //_appDbContext.SaveChanges();

    /*bool includeSold = true;
    var query = _appDbContext.Listings
      .OrderByDescending(l => l.DateOfListing)
      .Where(l => l.AgencyId == 1);

    if (!includeSold) query = query.Where(l => l.Status == ListingStatus.Listed);

    List<AgencyListingDTO> listings = await query
      .Select(l => new AgencyListingDTO
      {
        Address = 
        DateOfListing = l.DateOfListing.UtcDateTime,
        ListingURL = l.URL,
        Price = l.Price,
        Status = l.Status.ToString(),
        GeneratedLeadsCount = l.LeadsGenerated.Count,
        AssignedBrokers = l.BrokersAssigned.Select(b => new BrokerPerListingDTO
        {
          BrokerId = b.BrokerId,
          firstName = b.Broker.FirstName,
          lastName = b.Broker.LastName
        })
      }).AsNoTracking()
      .ToListAsync();*/


    //return Ok(listings);
    return Ok();
  }


  [HttpGet("createTemaplate")]
  public async Task<IActionResult> TestTemplate()
  {
    var CreateTemplateDto = new CreateTemplateDTO{subject = "this is emails subject", TemplateType = "e", text = "hello abdul wassup" };
    var template = await _templatesQService.CreateTemplateAsync(CreateTemplateDto, Guid.Parse("1B935034-6F92-41C7-99D0-A41181A7DF54"));
    return Ok();
  }

  [HttpGet("GetTemplates")]
  public async Task<IActionResult> TestGetTemplates()
  {

    var templatesDTO = await _templatesQService.GetAllTemplatesAsync(Guid.Parse("1B935034-6F92-41C7-99D0-A41181A7DF54"));
    return Ok();
  }

  [HttpGet("test-get/{id?}")]
  public async Task<IActionResult> TestGet(int? id = null)
  {
    
    Console.WriteLine(id);
    return Ok();
  }

  [HttpGet("test-create-leads")]
  public async Task<IActionResult> TestCreateLead()
  {
    _appDbContext.Leads.Add(new Lead
    {
      AgencyId =3,
      Budget = 1000,
      Email = "lol@hotmail123.com",
      EntryDate = DateTime.Now,
      LeadFirstName = "abdul",
      LeadLastName = "john",
      LeadStatus = LeadStatus.New,
      PhoneNumber = "513"
    });
    _appDbContext.Leads.Add(new Lead
    {
      AgencyId = 3,
      Budget = 10003,
      Email = "lol@hotmai32l.com",
      EntryDate = DateTime.Now,
      LeadFirstName = "sadfasdf",
      LeadLastName = "wal",
      LeadStatus = LeadStatus.New,
      PhoneNumber = "514"
    });
    _appDbContext.SaveChanges();
    return Ok();
  }
  [HttpGet("test-count")]
  public async Task<IActionResult> TestCount()
  {
    var res = _appDbContext.Agencies.Where(a => a.Id == 3)
      .Select(a => new { a.SignupDateTime, a.Leads.Count })
      .FirstOrDefault();

    Console.WriteLine(res.Count);
    return Ok();
  }

  [HttpPost("test-date")]
  public async Task<IActionResult> TestFster([FromBody] DateTestDTO dto)
  {
    var a = _appDbContext.Agencies.Where(a => a.Id == 3).FirstOrDefault();
    Console.WriteLine(a.SignupDateTime);
    return Ok();
  }

  [HttpGet("test-stuff")]
  public async Task<IActionResult> TestStuff()
  {
    //_logger.LogInformation("logging to see stuff lmao");
    //throw new BusinessRuleValidationException(new BrokerEmailsMustBeUniqueRule("bashEmail"));
    //throw new InconsistentStateException("test", "test message");
    //await _graphService.test();
    /*var area = new Area { Name = "myArea", PostalCode = 1 };
    var lead = new Lead
    {
      Budget = 1000,
      Email = "lol@hotmail.com",
      EntryDate = DateTime.Now,
      LeadFirstName = "abdul",
      LeadLastName = 3,
      LeadStatus = Status.Client,
      PhoneNumber = "513"
    };
    var agency = new Agency
    {
      AgencyName = "lolagency",
      NumberOfBrokersInDatabase = 1,
      NumberOfBrokersInSubscription = 0,
      StripeSubscriptionStatus = StripeSubscriptionStatus.NoStripeSubscription,
      Areas = new List<Area> { area },
      Leads = new List<Lead> { lead },
    };
    await _agencyRepo.AddAsync(agency);*/

    /*var spec = new AgencyByIdWithAreasAndLeads(1);
    var agency = await _agencyRepo.GetBySpecAsync(spec);
    agency.Leads[0].AreasOfInterest = new List<Area> { agency.Areas[0] };
    await _agencyRepo.UpdateAsync(agency);*/

    /*var spec = new AreaByIdWithLeads(1);
    var area = await _areaRepo.GetBySpecAsync(spec);
    area.InterestedLeads.RemoveAll(lead => lead.Id == 1);
    await _areaRepo.UpdateAsync(area);*/

    /*var spec = new LeadByIdWithAreas(1);
    var lead = await _leadRepo.GetBySpecAsync(spec);
    lead.AreasOfInterest.RemoveAll(a => a.Id == 1);
    await _leadRepo.UpdateAsync(lead);*/
    /*var lead = new Lead
    {
      Budget = 1000,
      Email = "lol@hotmail.com",
      EntryDate = DateTime.Now,
      LeadFirstName = "abdul",
      LeadLastName = "john",
      LeadStatus = Status.Client,
      PhoneNumber = "513"
    };
    var listing = new Listing
    {
      Address = "611 rue loo",
      Status = ListingStatus.Listed,
      DateOfListing = DateTime.UtcNow,
      Price = 1000
    };
    var agency = new Agency
    {
      AgencyName = "lolagency",
      NumberOfBrokersInDatabase = 1,
      NumberOfBrokersInSubscription = 0,
      StripeSubscriptionStatus = StripeSubscriptionStatus.NoStripeSubscription,
      Leads = new List<Lead> { lead },
      AgencyListings = new List<Listing> { listing}
    };
    await _agencyRepo.AddAsync(agency);*/

    /*var spec = new AgencyByIdWithLeadsAndListings(1);
    var agency = await _agencyRepo.GetBySpecAsync(spec);
    var listing = agency.AgencyListings[0];*/

    //var spec2 = new LeadByIdWithListings(1);
    //var lead = await _leadRepo.GetBySpecAsync(spec2);
    //lead.ListingsOfInterest.RemoveAll(x => x.ListingId ==1);
    //await _leadRepo.UpdateAsync(lead);
    //lead 1 notifs
    /*var notif1 = new Notification {
      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      NotifData = "this is email text1",
      NotifType = NotifType.EmailReceived,
      NotifyBroker = true,
      UnderlyingEventTimeStamp = DateTime.UtcNow,
    };
    var notif2 = new Notification
    {
      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      NotifData = "this is response to email 1",
      NotifType = NotifType.EmailSent,
      UnderlyingEventTimeStamp = DateTime.UtcNow.Add(new TimeSpan(0,20,0)),
    };
    var notif3 = new Notification
    {
      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      NotifData = "this is email text3",
      NotifType = NotifType.EmailReceived,
      NotifyBroker = true,
      UnderlyingEventTimeStamp = DateTime.UtcNow.Add(new TimeSpan(0, 30, 0)),
    };
    var notif4 = new Notification
    {
      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      NotifData = "this is email text3",
      NotifType = NotifType.CallReceived,
      NotifyBroker = true,
      UnderlyingEventTimeStamp = DateTime.UtcNow.Add(new TimeSpan(0, 40, 0)),
    };
    List<Notification> notifsLead1 = new() { notif1, notif2, notif3,notif4};
    var lead1 = new Lead
    {
      Budget = 1000,
      AgencyId = 2,
      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      LeadHistoryEvents = notifsLead1,
      Email = "lol@hotmail1.com",
      EntryDate = DateTime.Now,
      LeadFirstName = "abdul1",
      LeadLastName = "wal",
      LeadStatus = LeadStatus.New,
      PhoneNumber = "513"
    };*/
    /*
    var notif111 = new Notification
    {
      LeadId = 1,
      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      NotifType = NotifType.LeadStatusChange,
      NotifyBroker = true,
      EventTimeStamp = DateTime.UtcNow,
    };
    var notif11 = new Notification
    {
      LeadId = 1,
      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      //NotifData = "this is email text1 lead2",
      NotifType = NotifType.EmailReceived,
      NotifyBroker = true,
      UnderlyingEventTimeStamp = DateTime.UtcNow,
    };
    var notif22 = new Notification
    {
      LeadId = 1,

      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      //NotifData = "this is response to email 1 lead2",
      NotifType = NotifType.EmailSent,
      UnderlyingEventTimeStamp = DateTime.UtcNow.Add(new TimeSpan(0, 20, 0)),
    };
    var notif33 = new Notification
    {
      LeadId = 1,

      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      //NotifData = "this is email text3 lead2",
      NotifType = NotifType.EmailReceived,
      NotifyBroker = true,
      UnderlyingEventTimeStamp = DateTime.UtcNow.Add(new TimeSpan(0, 30, 0)),
    };
    var notif44 = new Notification
    {
      LeadId = 1,

      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      //NotifData = "this is email text3 lead2",
      NotifType = NotifType.CallReceived,
      NotifyBroker = true,
      UnderlyingEventTimeStamp = DateTime.UtcNow.Add(new TimeSpan(0, 40, 0)),
    };
    List<Notification> notifsLead11 = new() { notif111, notif11, notif22, notif33, notif44 };*/
    /*var lead2 = new Lead
    {
      Budget = 269000,
      AgencyId = 2,
      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      LeadHistoryEvents = notifsLead1,
      Email = "lol@hotmail2.com",
      EntryDate = DateTime.Now,
      LeadFirstName = "abdul2",
      LeadLastName = "wal",
      LeadStatus = LeadStatus.New,
      PhoneNumber = "514"
    };*/
   // _appDbContext.Notifications.AddRange(notifsLead11);
    _appDbContext.SaveChanges();

    return Ok();
  }

  [HttpGet("testgetnotifs")]
  public async Task<IActionResult> TestGetNotifs()
  {
    var id = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6");
    var DashboardNotifsDTO = await _mediator.Send(new GetNotifsDashboardRequest { BrokerId = id });
    return Ok();
  }

  [HttpGet("notifsLol")]
  public async Task<IActionResult> NotifsLol()
  {
    /*int notifTypeFilter = (int)(NotifType.EmailReceived | NotifType.SmsReceived | NotifType.CallReceived | NotifType.LeadStatusChange);

    var NotifWrappersList = await _appDbContext.Notifications
        .Where(n => n.BrokerId == Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6") && (notifTypeFilter & ((int)n.NotifType)) > 0)
        .GroupBy(n => n.LeadId)
        .Select(x => new WrapperNotifDashboard
        {
          leadId = (int)x.Key,
          notifs = x.OrderByDescending(n => n.UnderlyingEventTimeStamp)
            .Select(n => new NotifForDashboardDTO
            {
              NotifType = n.NotifType,
              ReadByBroker = n.ReadByBroker,
              //UnderlyingEventTimeStamp = n.UnderlyingEventTimeStamp
            })
        })
        .ToListAsync();
    int leadsNumber = NotifWrappersList.Count;
    List<int> leadIds = new(leadsNumber);
    NotifWrappersList.ForEach(x => leadIds.Add(x.leadId));
    var leadDTOs = await _appDbContext.Leads.Include(l => l.Tags)
      .AsSplitQuery()
      .OrderBy(l => l.Id)
      .Where(l => leadIds.Contains(l.Id))
      .Select(x => new DashboardLeadDTO
      {
        fstName = x.LeadFirstName,
        id = x.Id,
        lstName = x.LeadLastName,
        stsCode = x.LeadStatus,
        tags = x.Tags.Select(t => t.TagName) 
      }).ToListAsync();

    for (int i = 0; i < leadIds.Count; i++)
    {
      var leadDTO = leadDTOs[i];
      leadDTO.stsStr = leadDTO.stsCode.ToString();
      NotifWrappersList[i].leadDTO =leadDTO;
    }

    return Ok(NotifWrappersList);*/
    return Ok();
  }

  public class DateTestDTO
  {
    public DateTime ddate { get; set; }
  }

}
