using System;
using Core.Domain.ActionPlanAggregate.Actions;
using Core.Domain.ActionPlanAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.ExternalServiceInterfaces.ActionPlans;
using Infrastructure.Data;
using Web.Constants;

namespace Web.Processing.ActionPlans;

public class ActionExecuter: IActionExecuter
{
  private readonly AppDbContext _appDbContext;
  public ActionExecuter(AppDbContext appDbContext)
  {
    _appDbContext= appDbContext;
  }

  /// <summary>
  /// Returns true if continue processing, false stop right away dont need to
  /// 
  /// 0) ActionPlanAssociation - ActionPlanAssociation
  /// 1) List<ActionBase> - actions
  /// 2) Guid - brokerId
  /// 3) DateTime - timeNow
  /// </summary>
  /// <param name="pars"></param>
  /// <returns></returns>
  public async Task<bool> ExecuteChangeLeadStatus(params Object[] pars)
  {
    var ActionPlanAssociation = (ActionPlanAssociation)pars[0];
    var actions = (List<ActionBase>) pars[1];
    var brokerId = (Guid)pars[2];
    var timeNow = (DateTime)pars[3];

    var lead = ActionPlanAssociation.lead;
    var CurrentActionTracker = ActionPlanAssociation.ActionTrackers[0];
    var CurrentAction = actions[0];
    var currentActionLevel = CurrentAction.ActionLevel;

    string NewStatusString = CurrentAction.ActionProperties[ChangeLeadStatus.NewLeadStatus];

    Enum.TryParse<LeadStatus>(NewStatusString, true, out var NewLeadStatus);
    if (lead.LeadStatus == NewLeadStatus) return false;
    var oldStatus = lead.LeadStatus;
    lead.LeadStatus = NewLeadStatus;

    var StatusChangeNotif = new Notification
    {
      LeadId = lead.Id,
      BrokerId = brokerId,
      EventTimeStamp = timeNow,
      NotifType = NotifType.LeadStatusChange,
      ReadByBroker = false,
      NotifyBroker = true,
      IsActionPlanResult = true,
      //ProcessingStatus NO NEED for now, if later needs to be handled by outbox then assign
    };
    StatusChangeNotif.NotifProps[NotificationJSONKeys.ActionPlanId] = ActionPlanAssociation.ActionPlanId.ToString();
    StatusChangeNotif.NotifProps[NotificationJSONKeys.ActionId] = CurrentAction.Id.ToString();
    StatusChangeNotif.NotifProps[NotificationJSONKeys.APAssID] = ActionPlanAssociation.Id.ToString();

    StatusChangeNotif.NotifProps[NotificationJSONKeys.OldLeadStatus] = oldStatus.ToString();
    StatusChangeNotif.NotifProps[NotificationJSONKeys.NewLeadStatus] = lead.LeadStatus.ToString();
    _appDbContext.Notifications.Add(StatusChangeNotif);

    // TODO------------- signalR and push Notif

    return true;
  }

  /// <summary>
  /// Returns true if continue processing, false stop right away dont need to
  /// </summary>
  /// <param name="pars"></param>
  /// <returns></returns>
  /// <exception cref="NotImplementedException"></exception>
  public async Task<bool> ExecuteSendEmail(params Object[] pars)
  {
    Console.WriteLine("sending emaiiiiiiiiiiiiil\n");
    return true;
  }

  /// <summary>
  /// Returns true if continue processing, false stop right away dont need to
  /// </summary>
  /// <param name="pars"></param>
  /// <returns></returns>
  /// <exception cref="NotImplementedException"></exception>
  public Task<bool> ExecuteSendSms(params Object[] pars)
  {
    throw new NotImplementedException();
  }
}
