using Infrastructure.Data;
using Web.Outbox.Config;

namespace Web.Outbox;

//action plan started with any trigger.
//SignalR and Push Notif if triggered automatically
public class APStarted : EventBase
{
}
public class APStartedHandler : EventHandlerBase<APStarted>
{
  public APStartedHandler(AppDbContext appDbContext, ILogger<APStartedHandler> logger) : base(appDbContext, logger)
  {
  }

  public override async Task Handle(APStarted notification, CancellationToken cancellationToken)
  {
    _logger.LogWarning("handling action plan started event");
  }
}

