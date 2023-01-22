using Infrastructure.Data;
using Web.Outbox.Config;

namespace Web.Outbox;


public class LeadCreated : EventBase
{

}


public class LeadCreatedHandler : EventHandlerBase<LeadCreated>
{
  public LeadCreatedHandler(AppDbContext appDbContext, ILogger<LeadCreatedHandler> logger) : base(appDbContext, logger)
  {
  }

  public override Task Handle(LeadCreated notification, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}
