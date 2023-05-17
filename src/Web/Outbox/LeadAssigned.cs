using Infrastructure.Data;
using Web.Outbox.Config;

namespace Web.Outbox;

/// <summary>
/// when admin assign lead to broker or automation assigns lead to self, Handle ActionPlan or whatever if needed
/// especially when lead is created in-request. Also run signalR to notify broker of new lead if needed
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
        return Task.CompletedTask;
    }
}
