using Microsoft.AspNetCore.Mvc;

namespace Web.Api.TestingAPI;

[Route("api/[controller]")]
[ApiController]
public class TestProdController : ControllerBase
{
    [HttpGet("procCount")]
    public async Task<IActionResult> procCount()
    {
        var count = Environment.ProcessorCount;
        return Ok(count);
    }
}

