using Hangfire.Server;
using MediatR;
using Serilog.Context;

namespace Web.Outbox.Config;

public class OutboxDispatcher
{
    private readonly IMediator _mediator;
    public OutboxDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }
    public async Task Dispatch(EventBase Event, PerformContext performContext, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("appEventId", Event.AppEventId))
        using (LogContext.PushProperty("hanfireJobId", performContext.BackgroundJob.Id))
        {
            await _mediator.Publish(Event);
        }
    }
}
