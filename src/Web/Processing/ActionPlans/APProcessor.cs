using Core.Domain.ActionPlanAggregate;
using Core.Domain.BrokerAggregate.Templates;
using Core.Domain.NotificationAggregate;
using Core.ExternalServiceInterfaces.ActionPlans;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Web.Constants;

namespace Web.Processing.ActionPlans;

public class APProcessor
{
  private readonly AppDbContext _appDbContext;
  private readonly ILogger<APProcessor> _logger;
  public readonly IActionExecuter _actionExecuter;
  public APProcessor(AppDbContext appDbContext, IActionExecuter actionExecuter, ILogger<APProcessor> logger)
  {
    _appDbContext = appDbContext;
    _logger = logger;
    _actionExecuter = actionExecuter;
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
    var ActionPlanAssociation = await _appDbContext.ActionPlanAssociations
      .Include(ass => ass.ActionTrackers.Where(a => a.TrackedActionId == ActionId))
      .Include(ass => ass.lead)
      .ThenInclude(l => l.LeadEmails.Where(em => em.IsMain))
      .FirstAsync(ass => ass.LeadId == LeadId && ass.ActionPlanId == ActionPlanId);

    if (ActionPlanAssociation == null || !ActionPlanAssociation.ActionTrackers.Any() || ActionPlanAssociation.lead == null)
    {
      _logger.LogWarning("{location}: APAssociation is null for LeadId {LeadId} and ActionPlanId {ActionPlanId}", "APProcessor",LeadId,ActionPlanId);
      return;
    }
    var actions = await _appDbContext.Actions
      .Where(a => a.ActionPlanId == ActionPlanId && (a.ActionLevel == ActionLevel || a.ActionLevel == ActionLevel + 1))
      .OrderBy(a => a.ActionLevel)
      .AsNoTracking()
      .ToListAsync();

    var lead = ActionPlanAssociation.lead;
    var CurrentActionTracker = ActionPlanAssociation.ActionTrackers[0];
    var CurrentAction = actions[0];
    var currentActionLevel = CurrentAction.ActionLevel;

    if (CurrentActionTracker.ActionStatus != ActionStatus.ScheduledToStart)
    {
      _logger.LogError("{DoAction} processing action with id {ActionId} for lead {LeadId} with status {ActionTrackerStatus}", "doAction", ActionId, LeadId, CurrentActionTracker.ActionStatus.ToString());
      return;
    }
    Guid brokerId = lead.BrokerId ?? _appDbContext.ActionPlans.Select(a => new { a.BrokerId, a.Id }).Single(a => a.Id == ActionPlanId).BrokerId;

    var timeNow = DateTime.UtcNow;

    //START ----Specific Action Handling------------
    CurrentAction._IActionExecuter = _actionExecuter;
    //0) ActionPlanAssociation - ActionPlanAssociation
    //1) List < ActionBase > -actions
    //2) Guid - brokerId
    //3) DateTime - timeNow
    var ResTuple = await CurrentAction.Execute(ActionPlanAssociation, actions, brokerId, timeNow);
    if (!ResTuple.Item1) return;
    // END---------Specific action handling done --------


    // START---- Update current ActionTracker ----------
    CurrentActionTracker.ActionStatus = ActionStatus.Done;
    CurrentActionTracker.ExecutionCompletedTime = DateTime.UtcNow;
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

    bool saved = false;
    while (!saved)
    {
      try
      {
        // Attempt to save changes to the database
        await _appDbContext.SaveChangesAsync();
        saved = true;
      }
      catch (DbUpdateConcurrencyException ex)
      {
        foreach (var entry in ex.Entries)
        {
          if (entry.Entity is EmailTemplate || entry.Entity is SmsTemplate)
          {
            var proposedValues = entry.CurrentValues;
            var databaseValues = entry.GetDatabaseValues();

            databaseValues.TryGetValue("TimesUsed", out int dbcount);
            dbcount++;
            if (entry.Entity is EmailTemplate)
            {
              EmailTemplate emailTemplate = (EmailTemplate)entry.Entity;
              emailTemplate.TimesUsed = dbcount;
            }
            else
            {
              SmsTemplate smsTemplate = (SmsTemplate)entry.Entity;
              smsTemplate.TimesUsed = dbcount;
            }
            entry.OriginalValues.SetValues(databaseValues);
          }
          else
          {
            throw new NotSupportedException(
                "Don't know how to handle concurrency conflicts for "
                + entry.Metadata.Name);
          }
        }
      }
    }
    // END---- Handle Next Action DONE--------
  }
}
