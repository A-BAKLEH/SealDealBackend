using Clean.Architecture.Core.Domain.BrokerAggregate.Rules;
using Clean.Architecture.Core.Requests.AgencyRequests;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.BusinessRules;
using Clean.Architecture.SharedKernel.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.TestingAPI;
[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{

  private readonly IMediator _mediator;
  private readonly ILogger<TestController> _logger;
  public TestController(IMediator mediator, ILogger<TestController> logger)
  {
    _mediator = mediator;
    _logger = logger;
  }

  [HttpGet("test-signup")]
  public async Task<IActionResult> SigninSingupTest()
  {
    await _mediator.Send(new TestRequest1 { name = "abdul"});
    return Ok();
  }
  [HttpGet("test-stuff")]
  public async Task<IActionResult> TestStuff()
  {
    //_logger.LogInformation("logging to see stuff lmao");
    throw new BusinessRuleValidationException(new BrokerEmailsMustBeUniqueRule("bashEmail"));
    //throw new InconsistentStateException("test", "test message");
    return Ok();
  }
}
