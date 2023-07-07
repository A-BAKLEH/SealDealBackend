using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Web.Config;

namespace Web.Api.TestingAPI;

[AdminOnly]
[Route("api/[controller]")]
[ApiController]
public class LocalController : ControllerBase
{
    private readonly ILogger<LocalController> _logger;
    private readonly AppDbContext _dbcontext;

    public LocalController(ILogger<LocalController> logger, AppDbContext appDbContext)
    {
        _logger = logger;
        _dbcontext = appDbContext;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var count = _dbcontext.Brokers.Count();
        return Ok(count);
    }
}
