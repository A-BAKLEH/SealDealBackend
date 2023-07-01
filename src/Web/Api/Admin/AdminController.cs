using Core.Domain.TasksAggregate;
using Hangfire;
using Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.ControllerServices;
using Web.Outbox.Config;

namespace Web.Api.Admin;

[Authorize]
public class AdminController : BaseApiController
{
    private readonly AppDbContext appDbContext;
    public AdminController(AuthorizationService authorizeService,
        IMediator mediator, AppDbContext appDbContext1) : base(authorizeService, mediator)
    {
        appDbContext = appDbContext1;
    }

    //endpoints to refresh stripe and open ai keys
    //edpoints to schedule/check outboxdictionary

    [HttpPost("ScheduleOutboxDict")]
    public async Task<IActionResult> ScheduleOutboxDict()
    {
        var HangfireoutboxTaskId = Guid.NewGuid().ToString();
        RecurringJob.AddOrUpdate<OutboxCleaner>(HangfireoutboxTaskId, a => a.CleanOutbox(CancellationToken.None), $"* /5 * * * *");
        var outboxTask = new OutboxDictsTask { HangfireTaskId = HangfireoutboxTaskId };
        appDbContext.Add(outboxTask);
        await appDbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("CountOutboxDict")]
    public async Task<IActionResult> geteOutboxDictCount()
    {
        var c = OutboxMemCache.SchedulingErrorDict.Count;
        return Ok(c);
    }
}
