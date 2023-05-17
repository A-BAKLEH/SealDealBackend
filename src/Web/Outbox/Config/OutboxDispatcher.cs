using MediatR;

namespace Web.Outbox.Config;

public class OutboxDispatcher
{
    private readonly IMediator _mediator;
    public OutboxDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }
    public async Task Dispatch(EventBase Event)
    {
        await _mediator.Publish(Event);
    }
}
