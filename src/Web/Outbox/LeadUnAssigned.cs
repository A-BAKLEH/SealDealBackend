using Infrastructure.Data;
using Web.Outbox.Config;

namespace Web.Outbox;

public class LeadUnAssigned : EventBase
{
}
public class LeadUnAssignedHandler : EventHandlerBase<LeadUnAssigned>
{
  public LeadUnAssignedHandler(AppDbContext appDbContext, ILogger<LeadUnAssignedHandler> logger) : base(appDbContext, logger)
  {
  }

  public override Task Handle(LeadUnAssigned notification, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}
