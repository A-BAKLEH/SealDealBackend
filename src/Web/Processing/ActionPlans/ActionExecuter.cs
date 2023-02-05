using System;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.ExternalServiceInterfaces.ActionPlans;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Processing.ActionPlans;

public class ActionExecuter: IActionExecuter
{
  private readonly AppDbContext _appDbContext;
  public ActionExecuter(AppDbContext appDbContext)
  {
    _appDbContext= appDbContext;
  }

  public async Task ExecuteChangeLeadStatus()
  {
    int LeadId = 9;
    int ActionId = 32;
    int ActionPlanId = 2;

    var ActionPlanAssociation = await _appDbContext.ActionPlanAssociations
      .Include(ass => ass.ActionTrackers.First(a => a.TrackedActionId == ActionId))
      .ThenInclude(tracker => tracker.TrackedAction)
      .FirstAsync(ass => ass.LeadId == LeadId && ass.ActionPlanId == ActionPlanId);
    var action = ActionPlanAssociation.ActionTrackers[0].TrackedAction;
    //-----------------

    
  }

  public Task ExecuteSendEmail()
  {
    throw new NotImplementedException();
  }

  public Task ExecuteSendSms()
  {
    throw new NotImplementedException();
  }
}
