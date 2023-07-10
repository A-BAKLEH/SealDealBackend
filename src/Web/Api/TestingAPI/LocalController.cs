using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    [HttpGet("fixLeadStatuses")]
    public async Task<IActionResult> FixLeadStatus()
    {
        var appEvent = await _dbcontext.AppEvents.FirstAsync(e => e.Id == 79);
        appEvent.Props["OldLeadStatus"] = "Hot";
        _dbcontext.Entry(appEvent).State = EntityState.Modified;
        _dbcontext.Entry(appEvent).Property(e => e.Props).IsModified = true;
        await _dbcontext.SaveChangesAsync();
        await _dbcontext.Database.ExecuteSqlRawAsync
            (
              "UPDATE \"Leads\" SET \"LeadStatus\"='Hot' Where \"LeadStatus\"='New';"
            );
        return Ok();
    }
}
