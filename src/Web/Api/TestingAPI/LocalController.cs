﻿using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Web.Config;
using Web.ControllerServices.QuickServices;

namespace Web.Api.TestingAPI;

[AdminOnly]
[Route("api/[controller]")]
[ApiController]
public class LocalController : ControllerBase
{
    private readonly ILogger<LocalController> _logger;
    private readonly AppDbContext _dbcontext;
    private readonly MSFTEmailQService _MSFTEmailQService;

    public LocalController(ILogger<LocalController> logger,MSFTEmailQService mSFTEmailQService, AppDbContext appDbContext)
    {
        _logger = logger;
        _dbcontext = appDbContext;
        _MSFTEmailQService = mSFTEmailQService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var agency = _dbcontext.Agencies.FirstOrDefault(a => a.Id == 5);
        agency.NumberOfBrokersInSubscription = 2;
        await _dbcontext.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("DisconnectMsftLocal")]
    public async Task<IActionResult> DisconnectMsftLocal()
    {
        string email = "bashar.eskandar@sealdeal.ca";
        var id = Guid.Parse("88cc3a73-2e82-42ba-b86a-7396af053cce");
        await _MSFTEmailQService.DisconnectEmailMsftAsync(id, email);

        return Ok();
    }
}
