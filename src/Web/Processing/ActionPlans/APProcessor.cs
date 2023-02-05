using System.Reflection.Metadata;
using Core.Domain.ActionPlanAggregate;
using Core.Domain.ActionPlanAggregate.Actions;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Pipelines.Sockets.Unofficial.Arenas;
using Web.Constants;

namespace Web.Processing.ActionPlans;

public class APProcessor
{
  private readonly AppDbContext _appDbContext;
  private readonly ILogger<APProcessor> _logger;
  public APProcessor(AppDbContext appDbContext, ILogger<APProcessor> logger)
  {
    _appDbContext = appDbContext;
    _logger = logger;
  }

  /// <summary>
  /// Executes Action Plan action for a given ActionPlanAssociation and ActionTracker
  /// To be used when only leadId, ActionId and ActionPlanId are known, but not ActionPlanAssociationId
  /// </summary>
  /// <param name="LeadId"></param>
  /// <param name="ActionId"></param>
  /// <param name="ActionPlanId"></param>
  /// <returns></returns>
  public async Task DoActionAsync(int LeadId, int ActionId, byte ActionLevel, int ActionPlanId)
  {
    // want a query that will give me curret action and next one (where action level == currentLevel +1)
    // 
    var ActionPlanAssociationTask = _appDbContext.ActionPlanAssociations
      .Include(ass => ass.ActionTrackers.First(a => a.TrackedActionId == ActionId))
      .Include(ass => ass.lead)
      .FirstAsync(ass => ass.LeadId == LeadId && ass.ActionPlanId == ActionPlanId);

    var actionsTask = _appDbContext.Actions
      .Where(a => a.ActionPlanId == ActionPlanId && (a.ActionLevel == ActionLevel || a.ActionLevel == ActionLevel + 1))
      .OrderBy(a => a.ActionLevel)
      .AsNoTracking()
      .ToListAsync();

    var ActionPlanAssociation = await ActionPlanAssociationTask;
    var actions = await actionsTask;

    var lead = ActionPlanAssociation.lead;
    var CurrentActionTracker = ActionPlanAssociation.ActionTrackers[0];
    var CurrentAction = actions.Single(a => a.Id == ActionId && a.ActionLevel == ActionLevel);
    var currentActionLevel = CurrentAction.ActionLevel;

    Guid brokerId = lead.BrokerId ?? _appDbContext.ActionPlans.Select(a => new { a.BrokerId, a.Id }).Single(a => a.Id == ActionPlanId).BrokerId;

    var timeNow = DateTimeOffset.UtcNow;

    //START ----Specific Action Handling------------
    var atype = CurrentAction.GetType();

    //1) Change Lead Status Action
    // inputs : currentAction, lead, timeNow, ActionPlanId, actionId, APAssId, old new status
    if (atype == typeof(ChangeLeadStatus)) 
    {
      string NewStatusString = CurrentAction.ActionProperties[ChangeLeadStatus.NewLeadStatus];

      Enum.TryParse<LeadStatus>(NewStatusString, true, out var NewLeadStatus);
      if (lead.LeadStatus == NewLeadStatus) return;
      var oldStatus = lead.LeadStatus;
      lead.LeadStatus = NewLeadStatus;

      var StatusChangeNotif = new Notification
      {
        LeadId = LeadId,
        BrokerId = brokerId,
        EventTimeStamp = timeNow,
        NotifType = NotifType.LeadStatusChange,
        ReadByBroker = false,
        NotifyBroker = true,
        IsActionPlanResult = true,
        //ProcessingStatus NO NEED for now, if later needs to be handled by outbox then assign
      };
      StatusChangeNotif.NotifProps[NotificationJSONKeys.ActionPlanId] = ActionPlanId.ToString();
      StatusChangeNotif.NotifProps[NotificationJSONKeys.ActionId] = CurrentAction.Id.ToString();
      StatusChangeNotif.NotifProps[NotificationJSONKeys.APAssID] = ActionPlanAssociation.Id.ToString();

      StatusChangeNotif.NotifProps[NotificationJSONKeys.OldLeadStatus] = oldStatus.ToString();
      StatusChangeNotif.NotifProps[NotificationJSONKeys.NewLeadStatus] = lead.LeadStatus.ToString();
      _appDbContext.Notifications.Add(StatusChangeNotif);
      // TODO------------- signalR and push Notif
    }
    else if (atype == typeof(SendEmail))
    {

    }
    else if (atype == typeof(SendEmail))
    {

    }
    else
    {
      throw new NotImplementedException(CurrentAction.GetType().ToString());
    }
    // END---------Specific action handling done --------


    // START---- Update current ActionTracker ----------
    CurrentActionTracker.ActionStatus = ActionStatus.Done;
    CurrentActionTracker.ExecutionCompletedTime = timeNow;
    // END ---- Updating current ActionTracker Done -----------


    // START---- Handle Next Action ---------

    //If there is next action: enqueue it, create nextActionTracker, update current action
    //in APAssociation
    if (actions.Count == 2)
    {
      var NextAction = actions[1];
      var delays = CurrentAction.NextActionDelay?.Split(':');
      string NextHangfireJobId = "";

      TimeSpan timespan = TimeSpan.Zero;
      if (delays != null)
      {
        if (int.TryParse(delays[0], out var days)) timespan += TimeSpan.FromDays(days);
        if (int.TryParse(delays[1], out var hours)) timespan += TimeSpan.FromHours(hours);
        if (int.TryParse(delays[2], out var minutes)) timespan += TimeSpan.FromMinutes(minutes);
      }
      var NextActionTracker = new ActionTracker
      {
        TrackedActionId = NextAction.Id,
        ActionStatus = ActionStatus.ScheduledToStart,
        HangfireScheduledStartTime = timeNow + timespan,
      };
      ActionPlanAssociation.ActionTrackers.Add(NextActionTracker);
      ActionPlanAssociation.currentTrackedActionId = NextAction.Id;

      if (delays != null)
      {
        NextHangfireJobId = Hangfire.BackgroundJob.Schedule<APProcessor>(p => p.DoActionAsync(LeadId, NextAction.Id, NextAction.ActionLevel, ActionPlanId), timespan);
      }
      else
      {
        NextHangfireJobId = Hangfire.BackgroundJob.Enqueue<APProcessor>(p => p.DoActionAsync(LeadId, NextAction.Id, NextAction.ActionLevel, ActionPlanId));
      }
      NextActionTracker.HangfireJobId = NextHangfireJobId;
    }

    //Else update APAssociation Status
    // and create ActionPlanFinished notif
    else
    {
      ActionPlanAssociation.ThisActionPlanStatus = ActionPlanStatus.Done;
      var APDoneNotif = new Notification
      {
        LeadId = LeadId,
        BrokerId = brokerId,
        EventTimeStamp = timeNow,
        NotifType = NotifType.ActionPlanFinished,
        ReadByBroker = false,
        NotifyBroker = true,
        IsActionPlanResult = true,
        //ProcessingStatus NO NEED for now, if later needs to be handled by outbox then assign
      };
      APDoneNotif.NotifProps[NotificationJSONKeys.ActionPlanId] = ActionPlanId.ToString();
      APDoneNotif.NotifProps[NotificationJSONKeys.APFinishedReason] = NotificationJSONKeys.AllActionsCompleted;
      _appDbContext.Notifications.Add(APDoneNotif);

      // TODO SignalR and push notifs
    }
    await _appDbContext.SaveChangesAsync();
    // END---- Handle Next Action DONE--------
  }
}
