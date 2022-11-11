using Clean.Architecture.Core.Domain.ActionPlanAggregate;
using Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions;
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.Core.Domain.NotificationAggregate;
using Clean.Architecture.Core.DTOs.ProcessingDTOs;
using Clean.Architecture.Core.ExternalServiceInterfaces;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.SharedKernel.Exceptions;
using Clean.Architecture.Web.ApiModels.RequestDTOs;
using Clean.Architecture.Web.ControllerServices.QuickServices;
using Clean.Architecture.Web.MediatrRequests.AgencyRequests;
using Clean.Architecture.Web.MediatrRequests.NotifsRequests;
using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;

namespace Clean.Architecture.Web.Api.TestingAPI;
[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{

  private readonly IMediator _mediator;
  private readonly ILogger<TestController> _logger;
  private readonly IMsGraphService _graphService;

  private readonly AppDbContext _appDbContext;
  private readonly TemplatesQService _templatesQService;

  public TestController(IMediator mediator, ILogger<TestController> logger,
     AppDbContext appDbContext, IMsGraphService msGraphService, TemplatesQService templatesQService)
  {
    _mediator = mediator;
    _logger = logger;

    _appDbContext = appDbContext;
    _templatesQService = templatesQService;
  }

  [HttpGet("test-json")]
  public async Task<IActionResult> testJSON()
  {
   // _appDbContext.TestEntity1.Add();
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
  [HttpGet("test-azuread")]
  public async Task<IActionResult> azureadTest()
  {
    var id = Guid.Parse("1B935034-6F92-41C7-99D0-A41181A7DF54");
    var templatesDTO = await _templatesQService.GetAllTemplatesAsync(id);
    return Ok();
  }

  [HttpGet("test-signup")]
  public async Task<IActionResult> SigninSignupTest()
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
    return Ok();
  }

  [HttpGet("test-schema")]
  public async Task<IActionResult> TestNewSchema()
  {
    await _mediator.Send(new TestRequest1 { name = "abdul" });
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

    var notif111 = new Notification
    {
      LeadId = 1,
      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      NotifType = NotifType.LeadStatusChange,
      NotifyBroker = true,
      UnderlyingEventTimeStamp = DateTime.UtcNow,
    };
    var notif11 = new Notification
    {
      LeadId = 1,
      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      NotifData = "this is email text1 lead2",
      NotifType = NotifType.EmailReceived,
      NotifyBroker = true,
      UnderlyingEventTimeStamp = DateTime.UtcNow,
    };
    var notif22 = new Notification
    {
      LeadId = 1,

      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      NotifData = "this is response to email 1 lead2",
      NotifType = NotifType.EmailSent,
      UnderlyingEventTimeStamp = DateTime.UtcNow.Add(new TimeSpan(0, 20, 0)),
    };
    var notif33 = new Notification
    {
      LeadId = 1,

      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      NotifData = "this is email text3 lead2",
      NotifType = NotifType.EmailReceived,
      NotifyBroker = true,
      UnderlyingEventTimeStamp = DateTime.UtcNow.Add(new TimeSpan(0, 30, 0)),
    };
    var notif44 = new Notification
    {
      LeadId = 1,

      BrokerId = Guid.Parse("BC3F8BAE-0E21-4DE9-B7B0-EB9176CDB8E6"),
      NotifData = "this is email text3 lead2",
      NotifType = NotifType.CallReceived,
      NotifyBroker = true,
      UnderlyingEventTimeStamp = DateTime.UtcNow.Add(new TimeSpan(0, 40, 0)),
    };
    List<Notification> notifsLead11 = new() { notif111, notif11, notif22, notif33, notif44 };
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
    _appDbContext.Notifications.AddRange(notifsLead11);
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
    int notifTypeFilter = (int)(NotifType.EmailReceived | NotifType.SmsReceived | NotifType.CallReceived | NotifType.LeadStatusChange);

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
              UnderlyingEventTimeStamp = n.UnderlyingEventTimeStamp
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

    return Ok(NotifWrappersList);
  }

  public class DateTestDTO
  {
    public DateTime ddate { get; set; }
  }

}
