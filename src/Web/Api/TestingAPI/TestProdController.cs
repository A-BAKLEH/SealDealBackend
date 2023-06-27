using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace Web.Api.TestingAPI;

[Route("api/[controller]")]
[ApiController]
public class TestProdController : ControllerBase
{
    private IConfigurationSection section;
    private readonly IWebHostEnvironment _webHostEnv;
    private readonly AppDbContext appDbContext;
    private readonly ILogger<TestProdController> _logger;
    public TestProdController(IConfiguration configuration,
        AppDbContext dbContext,
        ILogger<TestProdController> logger,
        IWebHostEnvironment webHostEnv)
    {
        section = configuration.GetSection("Hangfire");
        _webHostEnv = webHostEnv;
        appDbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("lols")]
    public async Task<IActionResult> lols()
    {
        var count = await appDbContext.Agencies.CountAsync ();
        return Ok(count);
    }

    [HttpGet("logs")]
    public async Task<IActionResult> logst()
    {
        _logger.LogInformation("{tag} hello logs loggies.", "placeFromTagLol");
        return Ok();
    }

    [HttpGet("procCount")]
    public async Task<IActionResult> procCount()
    {
        var count = Environment.ProcessorCount;
        return Ok(count);
    }

    [HttpGet("envvars")]
    public async Task<IActionResult> envvars()
    {
        var userName = section["Username"];
        return Ok(userName);
    }
    [HttpGet("env")]
    public async Task<IActionResult> env()
    {
        var c = _webHostEnv.EnvironmentName;
        return Ok(c);
    }
}

