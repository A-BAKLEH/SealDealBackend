using Clean.Architecture.Core.Domain.ActionPlanAggregate;
using Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions;
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.NotificationAggregate;
using Clean.Architecture.Core.ExternalServiceInterfaces;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Web.Cache.Extensions;
using Clean.Architecture.Web.MediatrRequests.AgencyRequests;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Web.Api.TestingAPI;
[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{

  private readonly IMediator _mediator;
  private readonly ILogger<TestController> _logger;
  private readonly IMsGraphService _graphService;

  private readonly AppDbContext _appDbContext;
  public TestController(IMediator mediator, ILogger<TestController> logger,
     AppDbContext appDbContext,IMsGraphService msGraphService)
  {
    _mediator = mediator;
    _logger = logger;

    _appDbContext = appDbContext;
  }

  [HttpGet("test-stream")]
  public async Task<IActionResult> StreamdTest()
  {
    var agency = new Agency {Id = 3, AdminStripeId = "stripeid" };
    var bytes = agency.ToByteArray<Agency>();

    var a1 = bytes.FromByteArray<Agency>();

    return Ok();
  }


  [HttpGet("test-azuread")]
  public async Task<IActionResult> azureadTest()
  {
    
    return Ok();
  }

  [HttpGet("test-signup")]
  public async Task<IActionResult> SigninSignupTest()
  {
    var agency = _appDbContext.Agencies.Include(a => a.AgencyBrokers).ThenInclude(b => b.ActionPlans).FirstOrDefault(a => a.Id == 1);
    var emailtemp = new EmailTemplate
    {EmailTemplateSubject = "wlak", EmailTemplateText = "salut habibi qu'est ce pasee" };
    agency.AgencyBrokers[0].EmailTemplates = new List<EmailTemplate> { emailtemp };
    var act1 = new SendEmailAction
    {
      ActionLevel = 1,
      NextActionDelay = "1:0:0",
    };
    act1.ActionProperties[SendEmailAction.EmailTemplateIdKey] = "1";

    var actionplan = new ActionPlan
    {
      ActionsCount = 1,
      AssignToLead = true,
      isActive = true,
      StopPlanOnInteraction = true,
      Title = "first plan",
      NotifsToListenTo = NotifType.EmailReceived | NotifType.SmsReceived | NotifType.Call,
      Triggers = NotifType.LeadAssigned,
      Actions = new List<ActionBase>
      {
        act1
      }
    };
    agency.AgencyBrokers[0].ActionPlans.Add(actionplan);
    _appDbContext.SaveChanges();
    return Ok();
  }

  [HttpGet("test-schema")]
  public async Task<IActionResult> TestNewSchema()
  {
    await _mediator.Send(new TestRequest1 { name = "abdul" });
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

    return Ok();
  }
}
