using Infrastructure.Data;
using Web.Outbox.Config;

namespace Web.Outbox;

/// <summary>
/// 
/// </summary>
public class LeadCreated : EventBase
{

}

/// <summary>
/// If lead created manually by broker do nothing
/// If lead created by admin and assigned to Broker: Notify Broker by SignalR and pushNotif      
/// </summary>
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
