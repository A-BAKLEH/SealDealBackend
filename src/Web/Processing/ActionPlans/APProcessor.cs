using Core.Config.Constants.LoggingConstants;
using Core.Domain.ActionPlanAggregate;
using Core.Domain.BrokerAggregate.Templates;
using Core.Domain.NotificationAggregate;
using Core.DTOs.ProcessingDTOs;
using Hangfire;
using Hangfire.Server;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Web.Constants;
using Web.RealTimeNotifs;

namespace Web.Processing.ActionPlans;

public class APProcessor
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<APProcessor> _logger;
    public readonly ActionExecuter _actionExecuter;
    public APProcessor(AppDbContext appDbContext, ActionExecuter actionExecuter, ILogger<APProcessor> logger)
    {
        _appDbContext = appDbContext;
        _logger = logger;
        _actionExecuter = actionExecuter;
    }
    /// <summary>
    /// Executes Action Plan action for a given ActionPlanAssociation and ActionTracker
    /// executes signalR and push notif since called in background
    /// </summary>
    /// <param name="LeadId"></param>
    /// <param name="ActionId"></param>
    /// <param name="ActionPlanId"></param>
    /// <returns></returns>
    [DisableConcurrentExecution(300)]
    public async Task DoActionAsync(int LeadId, int ActionId, byte ActionLevel, int ActionPlanId, PerformContext performContext)
    {
        var ActionPlanAssociation = await _appDbContext.ActionPlanAssociations
            .Include(ass => ass.ActionPlan)
            .Include(ass => ass.ActionTrackers.Where(a => a.TrackedActionId == ActionId))
            .Include(ass => ass.lead)
            .ThenInclude(l => l.LeadEmails.Where(em => em.IsMain))
            .FirstOrDefaultAsync(ass => ass.LeadId == LeadId && ass.ActionPlanId == ActionPlanId);

        if (ActionPlanAssociation == null || !ActionPlanAssociation.ActionTrackers.Any() || ActionPlanAssociation.lead == null)
        {
            _logger.LogWarning("{tag} APAssociation is null for LeadId {leadId} and ActionPlanId {actionPlanId}", TagConstants.doAction, LeadId, ActionPlanId);
            return;
        }
        var actions = await _appDbContext.Actions
          .Where(a => a.ActionPlanId == ActionPlanId && (a.ActionLevel == ActionLevel || a.ActionLevel == ActionLevel + 1))
          .Select(a => new ActionExecutingDTO { nextActionDelay = a.NextActionDelay, dataTemplateId = a.DataTemplateId, ActionLevel = a.ActionLevel, Id = a.Id, ActionProperties = a.ActionProperties, ActionType = a.ActionType, BrokerId = a.ActionPlan.BrokerId })
          .OrderBy(a => a.ActionLevel)
          .AsNoTracking()
          .ToListAsync();

        var lead = ActionPlanAssociation.lead;
        var CurrentActionTracker = ActionPlanAssociation.ActionTrackers[0];
        var CurrentAction = actions[0];
        var currentActionLevel = CurrentAction.ActionLevel;

        if (CurrentActionTracker.ActionStatus != ActionStatus.ScheduledToStart)
        {
            _logger.LogError("{tag} processing action with id {actionId} for lead {leadId} with status {actionTrackerStatus}", TagConstants.doAction, ActionId, LeadId, CurrentActionTracker.ActionStatus.ToString());
            return;
        }

        if (CurrentActionTracker.HangfireJobId == null) CurrentActionTracker.HangfireJobId = performContext.BackgroundJob.Id;
        Guid brokerId = lead.BrokerId ?? actions[0].BrokerId;

        var timeNow = DateTime.UtcNow;

        //START ----Specific Action Handling------------
        Tuple<bool, AppEvent?> res = null;
        if (CurrentAction.ActionType == ActionType.ChangeLeadStatus)
        {
            res = _actionExecuter.ExecuteChangeLeadStatus(ActionPlanAssociation, CurrentAction, brokerId, timeNow);
        }
        else if (CurrentAction.ActionType == ActionType.SendSms)
        {
            res = await _actionExecuter.ExecuteSendSms(ActionPlanAssociation, CurrentAction, brokerId, timeNow);
        }
        else if (CurrentAction.ActionType == ActionType.SendEmail)
        {
            res = await _actionExecuter.ExecuteSendEmail(ActionPlanAssociation, CurrentAction, brokerId, timeNow);
        }
        if (!res.Item1) return;
        // END---------Specific action handling done --------

        var RTEvents = new List<AppEvent>(2) { res.Item2 };

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
            var delays = CurrentAction.nextActionDelay?.Split(':');
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
                NextHangfireJobId = Hangfire.BackgroundJob.Schedule<APProcessor>(p => p.DoActionAsync(LeadId, NextAction.Id, NextAction.ActionLevel, ActionPlanId, null), timespan);
            }
            else
            {
                NextHangfireJobId = Hangfire.BackgroundJob.Enqueue<APProcessor>(p => p.DoActionAsync(LeadId, NextAction.Id, NextAction.ActionLevel, ActionPlanId, null));
            }
            NextActionTracker.HangfireJobId = NextHangfireJobId;
        }

        //Else update APAssociation Status
        // and create ActionPlanFinished notif
        else
        {
            ActionPlanAssociation.ThisActionPlanStatus = ActionPlanStatus.Done;
            var APDoneEvent = new AppEvent
            {
                LeadId = LeadId,
                BrokerId = brokerId,
                EventTimeStamp = timeNow,
                EventType = EventType.ActionPlanFinished,
                ReadByBroker = false,
                IsActionPlanResult = true,
                ProcessingStatus = ProcessingStatus.NoNeed
            };
            APDoneEvent.Props[NotificationJSONKeys.ActionPlanId] = ActionPlanId.ToString();
            APDoneEvent.Props[NotificationJSONKeys.ActionPlanName] = ActionPlanAssociation.ActionPlan.Name;
            APDoneEvent.Props[NotificationJSONKeys.APFinishedReason] = NotificationJSONKeys.AllActionsCompleted;
            _appDbContext.AppEvents.Add(APDoneEvent); 
            RTEvents.Add(APDoneEvent);
        }

        bool saved = false;
        byte c = 0;
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
                if (c >= 3) throw;
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
                c++;
                await Task.Delay(200);
            }
        }
        // END---- Handle Next Action DONE--------

        // SignalR and push notifs for action-result event + endofActionPlan event
        await RealTimeNotifSender.SendRealTimeNotifsAsync(_logger,brokerId, true, true, null, RTEvents, null);  
    }
}
