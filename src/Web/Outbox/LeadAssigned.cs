using Infrastructure.Data;
using Web.Outbox.Config;

namespace Web.Outbox;

/// <summary>
/// when admin assign lead to broker
/// SignalR and Push Notif
/// </summary>
public class LeadAssigned : EventBase
{
}
public class LeadAssignedHandler : EventHandlerBase<LeadAssigned>
{
  public LeadAssignedHandler(AppDbContext appDbContext, ILogger<LeadAssignedHandler> logger) : base(appDbContext, logger)
  {
  }

  public override Task Handle(LeadAssigned notification, CancellationToken cancellationToken)
  {
    Console.WriteLine("la3end allah");
    return Task.CompletedTask;
  }
}
