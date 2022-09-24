using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.Core.Domain.LeadAggregate.Specifications;
using Clean.Architecture.Core.ExternalServiceInterfaces;
using Clean.Architecture.Core.MediatrRequests.AgencyRequests;
using Clean.Architecture.SharedKernel.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.TestingAPI;
[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{

  private readonly IMediator _mediator;
  private readonly ILogger<TestController> _logger;
  private readonly IMsGraphService _graphService;

  private readonly IRepository<Lead> _leadRepo;
  private readonly IRepository<Broker> _brokerRepo;
  private readonly IRepository<Agency> _agencyRepo;
  private readonly IRepository<Area> _areaRepo;
  public TestController(IMediator mediator, ILogger<TestController> logger, IMsGraphService msGraphService,
    IRepository<Agency> agencyRepo, IRepository<Lead> leadRepo, IRepository<Broker> brokerRepo, IRepository<Area> areaRepo)
  {
    _mediator = mediator;
    _logger = logger;
    _graphService = msGraphService;
    _agencyRepo = agencyRepo;
    _leadRepo = leadRepo;
    _brokerRepo = brokerRepo;
    _areaRepo = areaRepo;
  }


  [HttpGet("test-azuread")]
  public async Task<IActionResult> azureadTest()
  {
    
    return Ok();
  }

  [HttpGet("test-signup")]
  public async Task<IActionResult> SigninSingupTest()
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

    var spec2 = new LeadByIdWithListings(1);
    var lead = await _leadRepo.GetBySpecAsync(spec2);
    lead.ListingsOfInterest.RemoveAll(x => x.ListingId ==1);
    await _leadRepo.UpdateAsync(lead);

    return Ok();
  }
}
