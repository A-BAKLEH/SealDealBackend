using Core.Config.Constants.LoggingConstants;
using Core.Constants.ProblemDetailsTitles;
using Core.Domain.ActionPlanAggregate;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.DTOs.ProcessingDTOs;
using Hangfire;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Exceptions;
using Web.ApiModels.APIResponses.ActionPlans;
using Web.ApiModels.RequestDTOs.ActionPlans;
using Web.Config.EnumExtens;
using Web.Constants;
using Web.Processing.ActionPlans;

namespace Web.ControllerServices.QuickServices;

public class ActionPQService
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<ActionPQService> _logger;
    public ActionPQService(ILogger<ActionPQService> logger, AppDbContext appDbContext)
    {
        _logger = logger;
        _appDbContext = appDbContext;
    }

    public async Task SetNewTriggerAsync(Guid brokerID, string NewTrigger, int ActionPlanId)
    {
        EventType trigger;
        switch (NewTrigger)
        {
            case "LeadAssigned":
                trigger = EventType.LeadAssignedToYou;
                break;
            case "Manual":
                trigger = EventType.None;
                break;
            default:
                throw new CustomBadRequestException("invalid trigger", ProblemDetailsTitles.InvalidInput);
        };

        var broker = await _appDbContext.Brokers
            .Include(b => b.ActionPlans)
            .FirstAsync(b => b.Id == brokerID);

        var thisActionPlan = broker.ActionPlans.First(ap => ap.Id == ActionPlanId);
        if (trigger == EventType.LeadAssignedToYou)
        {         
            if (thisActionPlan.isActive && broker.ActionPlans.Any(ap => ap.Id != ActionPlanId && ap.isActive && ap.Triggers.HasFlag(EventType.LeadAssignedToYou)))
            {
                throw new CustomBadRequestException("there is at least one other workflow that has an automatic trigger of" +
                    "LeadAssigned and is active", ProblemDetailsTitles.InvalidInput);
            }
            thisActionPlan.StopPlanOnInteraction = true;
        }
        thisActionPlan.Triggers = trigger;
        
        if (!broker.ListenForActionPlans.HasFlag(trigger))
        {
            broker.ListenForActionPlans |= trigger;
        }
        await _appDbContext.SaveChangesAsync();
    }
    public async Task ToggleActiveTriggerAsync(Guid brokerId, bool Toggle, int ActionPlanId)
    {
        if (Toggle == false)
        {
            var ActionPlanAssociations = await _appDbContext.ActionPlanAssociations
                .Where(apa => apa.ActionPlanId == ActionPlanId && apa.ActionPlan.BrokerId == brokerId && apa.ThisActionPlanStatus == ActionPlanStatus.Running)
                .Include(apa => apa.ActionTrackers.Where(a => a.ActionStatus == ActionStatus.ScheduledToStart || a.ActionStatus == ActionStatus.Failed))
                .ToListAsync();

            var actionPlanName = await _appDbContext.ActionPlans
                .Select(a => new { a.Name, a.Id })
                .FirstAsync(a => a.Id == ActionPlanId);

            foreach (var apass in ActionPlanAssociations)
            {
                var APDoneEvent = new AppEvent
                {
                    LeadId = apass.LeadId,
                    BrokerId = brokerId,
                    EventTimeStamp = DateTime.UtcNow,
                    EventType = EventType.ActionPlanFinished,
                    ReadByBroker = true,
                    IsActionPlanResult = true,
                    ProcessingStatus = ProcessingStatus.NoNeed
                };
                APDoneEvent.Props[NotificationJSONKeys.ActionPlanId] = ActionPlanId.ToString();
                APDoneEvent.Props[NotificationJSONKeys.ActionPlanName] = actionPlanName.Name;
                APDoneEvent.Props[NotificationJSONKeys.APFinishedReason] = NotificationJSONKeys.CancelledByBroker;
                _appDbContext.AppEvents.Add(APDoneEvent);

                apass.ThisActionPlanStatus = ActionPlanStatus.Cancelled;
                if (apass.ActionTrackers.Any())
                {
                    foreach (var ta in apass.ActionTrackers)
                    {
                        ta.ActionStatus = ActionStatus.Cancelled;
                        var jobId = ta.HangfireJobId;
                        if (jobId != null)
                            try
                            {
                                BackgroundJob.Delete(jobId);
                            }
                            catch (Exception) { }
                    }
                }
            }
        }
        else
        {
            var broker = await _appDbContext.Brokers
            .Include(b => b.ActionPlans)
            .FirstAsync(b => b.Id == brokerId);
            if(broker.ActionPlans.Any(ap => ap.Id != ActionPlanId && ap.isActive && ap.Triggers.HasFlag(EventType.LeadAssignedToYou)))
            {
                throw new CustomBadRequestException("there is at least one other active workflow that has an automatic trigger of" +
                    "Lead Assigned", ProblemDetailsTitles.InvalidInput);
            }
            if (!broker.ListenForActionPlans.HasFlag(broker.ActionPlans.First(ap => ap.Id == ActionPlanId).Triggers))
            {
                broker.ListenForActionPlans |= broker.ActionPlans[0].Triggers;
            }
        }
        await _appDbContext.ActionPlans
            .Where(a => a.Id == ActionPlanId && a.BrokerId == brokerId)
            .ExecuteUpdateAsync(setters =>
        setters.SetProperty(a => a.isActive, Toggle));
        await _appDbContext.SaveChangesAsync();
    }

    public async Task StopActionPlansOnALead(Guid brokerId, int LeadId)
    {
        var ActionPlanAssociations = await _appDbContext.ActionPlanAssociations
            .Include(apa => apa.ActionPlan)
            .Include(apa => apa.ActionTrackers.Where(a => a.ActionStatus == ActionStatus.ScheduledToStart || a.ActionStatus == ActionStatus.Failed))
            .Where(apa => apa.LeadId == LeadId && apa.ActionPlan.BrokerId == brokerId && apa.ThisActionPlanStatus == ActionPlanStatus.Running)
            .ToListAsync();

        foreach (var apass in ActionPlanAssociations)
        {
            var APDoneEvent = new AppEvent
            {
                LeadId = apass.LeadId,
                BrokerId = brokerId,
                EventTimeStamp = DateTime.UtcNow,
                EventType = EventType.ActionPlanFinished,
                ReadByBroker = true,
                IsActionPlanResult = true,
                ProcessingStatus = ProcessingStatus.NoNeed
            };
            APDoneEvent.Props[NotificationJSONKeys.ActionPlanId] = apass.ActionPlanId.ToString();
            APDoneEvent.Props[NotificationJSONKeys.ActionPlanName] = apass.ActionPlan.Name;
            APDoneEvent.Props[NotificationJSONKeys.APFinishedReason] = NotificationJSONKeys.CancelledByBroker;
            _appDbContext.AppEvents.Add(APDoneEvent);

            apass.ThisActionPlanStatus = ActionPlanStatus.Cancelled;
            if (apass.ActionTrackers.Any())
            {
                foreach (var ta in apass.ActionTrackers)
                {
                    ta.ActionStatus = ActionStatus.Cancelled;
                    var jobId = ta.HangfireJobId;
                    if (jobId != null)
                        try
                        {
                            BackgroundJob.Delete(jobId);
                        }
                        catch (Exception) { }
                }
            }
        }
        await _appDbContext.SaveChangesAsync();
    }
    public async Task<List<ActionPlanDTO>> GetMyActionPlansAsync(Guid brokerId)
    {
        var actionPlans = await _appDbContext.ActionPlans
          .Where(a => a.BrokerId == brokerId)
          .Select(a => new ActionPlanDTO
          {
              id = a.Id,
              FirstActionDelay = a.FirstActionDelay,
              isActive = a.isActive,
              name = a.Name,
              ActionsCount = a.ActionsCount,
              StopPlanOnInteraction = a.StopPlanOnInteraction,
              TimeCreated = a.TimeCreated,
              FlagTriggers = a.Triggers,
              TimesUsed = a.TimesUsed,
              TimesSuccess = a.TimesSuccess,
              Actions = a.Actions.OrderBy(a => a.ActionLevel).Select(aa => new ActionDTO
              {
                  actionType = aa.ActionType.ToString(),
                  ActionLevel = aa.ActionLevel,
                  ActionProperties = aa.ActionProperties,
                  NextActionDelay = aa.NextActionDelay,
                  TemplateId = aa.DataTemplateId
              }),
              ActiveOnXLeads = a.ActionPlanAssociations.Where(apa => apa.ThisActionPlanStatus == ActionPlanStatus.Running).Count(),
              leads = a.ActionPlanAssociations.Where(apa => apa.ThisActionPlanStatus == ActionPlanStatus.Running).Select(apa => new LeadNameIdDTO { LeadId = apa.LeadId, firstName = apa.lead.LeadFirstName, lastName = apa.lead.LeadLastName })
          })
          .OrderByDescending(a => a.ActiveOnXLeads)
          .ToListAsync();
        var TemplateIds = actionPlans.SelectMany(a => a.Actions).Select(a => a.TemplateId).Where(t => t != null).Distinct().ToList();
        if (TemplateIds.Any())
        {
            var templateNames = await _appDbContext.Templates
                .Where(t => t.BrokerId == brokerId && TemplateIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Title })
                .ToListAsync();
            foreach (var actionPlan in actionPlans)
            {
                foreach (var action in actionPlan.Actions)
                {
                    if (action.TemplateId != null) action.TemplateName = templateNames.First(t => t.Id == action.TemplateId).Title;
                }
            }
        }
        foreach (var item in actionPlans)
        {
            //TECH
            item.Triggers = item.FlagTriggers.GetIndividualFlags().Select(f => f.ToString()).ToList();
        }
        return actionPlans;
    }
    public async Task<ActionPlan1DTO> CreateActionPlanAsync(CreateActionPlanDTO dto, Guid brokerId)
    {
        var connEmailExists = await _appDbContext.ConnectedEmails.AnyAsync(e => e.BrokerId == brokerId && e.hasAdminConsent);
        if (!connEmailExists) throw new CustomBadRequestException("You do not have a connected Email for Automation", "NoConnectedEmail");
        EventType trigger;
        switch (dto.Trigger)
        {
            case "LeadAssigned":
                trigger = EventType.LeadAssignedToYou;
                break;
            case "Manual":
                trigger = EventType.None;
                break;
            default:
                throw new CustomBadRequestException("invalid trigger", ProblemDetailsTitles.InvalidInput);
        };
        if(trigger == EventType.LeadAssignedToYou && dto.ActivateNow)
        {
            var otherActionPlanExists = await _appDbContext.ActionPlans
                .Where(ap => ap.BrokerId == brokerId && ap.isActive && ap.Triggers.HasFlag(EventType.LeadAssignedToYou))
                .AnyAsync();
            if(otherActionPlanExists)
            {
                throw new CustomBadRequestException("you can only have one active action plan that is triggered by lead assignment", ProblemDetailsTitles.InvalidInput);
            }
        }
        if (dto.FirstActionDelay != null)
        {
            var delays = dto.FirstActionDelay.Trim().Split(':');
            int lol = 0;
            if (delays.Count() != 3 || !int.TryParse(delays[0], out lol) || !int.TryParse(delays[1], out lol) || !int.TryParse(delays[2], out lol))
            {
                throw new CustomBadRequestException($"input {dto.FirstActionDelay}", ProblemDetailsTitles.InvalidInput);
            }
        }
        var actionPlan = new ActionPlan
        {
            BrokerId = brokerId,
            Triggers = trigger,
            TimeCreated = DateTime.UtcNow,
            isActive = dto.ActivateNow,
            ActionsCount = (byte)dto.Actions.Count,
            Name = dto.Name,
            StopPlanOnInteraction = dto.StopPlanOnInteraction,
            FirstActionDelay = dto.FirstActionDelay,
            Actions = new()
        };

        foreach (var actionDTO in dto.Actions)
        {
            if (!Enum.TryParse<ActionType>(actionDTO.ActionType, true, out var actionType))
                throw new CustomBadRequestException($"Invalid action {actionDTO.ActionType}", ProblemDetailsTitles.InvalidInput);
            ActionPlanAction action = new()
            {
                ActionType = actionType,

            };
            switch (actionType)
            {
                case ActionType.SendEmail:
                    if (actionDTO.TemplateId == null) throw new CustomBadRequestException("TemplateId empty", ProblemDetailsTitles.InvalidInput);
                    action.DataTemplateId = actionDTO.TemplateId;
                    break;
                case ActionType.ChangeLeadStatus:
                    var InputStatus = actionDTO.Properties[ActionPlanAction.NewLeadStatus];
                    if (!Enum.TryParse<LeadStatus>(InputStatus, true, out var NewLeadStatus))
                        throw new CustomBadRequestException($"invalid Lead Status '{InputStatus}'", ProblemDetailsTitles.InvalidInput);
                    action.ActionProperties[ActionPlanAction.NewLeadStatus] = NewLeadStatus.ToString();
                    break;
                case ActionType.SendSms:
                    throw new CustomBadRequestException($"Invalid action {actionDTO.ActionType}", ProblemDetailsTitles.InvalidInput);
                //if (actionDTO.TemplateId == null) throw new CustomBadRequestException("TemplateId empty", ProblemDetailsTitles.InvalidInput);
                //action.DataTemplateId = actionDTO.TemplateId;
                //break;
                default:
                    throw new CustomBadRequestException($"Invalid action {actionDTO.ActionType}", ProblemDetailsTitles.InvalidInput);
            }

            var delays = actionDTO.NextActionDelay.Trim().Split(':');
            int lol = 0;
            if (delays.Count() != 3 || !int.TryParse(delays[0], out lol) || !int.TryParse(delays[1], out lol) || !int.TryParse(delays[2], out lol))
            {
                throw new CustomBadRequestException($"input {dto.FirstActionDelay}", ProblemDetailsTitles.InvalidInput);
            }

            action.ActionLevel = (byte)actionDTO.ActionLevel;
            action.NextActionDelay = actionDTO.NextActionDelay;
            actionPlan.Actions.Add(action);
        }

        //if action plan has an automatic trigger, add it to broker's NotifsForActionPlans
        if (actionPlan.Triggers != EventType.None && dto.ActivateNow)
        {
            var broker = await _appDbContext.Brokers.FirstAsync(b => b.Id == brokerId);
            if (!broker.ListenForActionPlans.HasFlag(actionPlan.Triggers))
            {
                broker.ListenForActionPlans |= actionPlan.Triggers;
            }
        }
        _appDbContext.ActionPlans.Add(actionPlan);
        await _appDbContext.SaveChangesAsync();

        var result = new ActionPlan1DTO
        {
            ActionsCount = actionPlan.ActionsCount,
            FirstActionDelay = actionPlan.FirstActionDelay,
            isActive = actionPlan.isActive,
            id = actionPlan.Id,
            name = actionPlan.Name,
            StopPlanOnInteraction = actionPlan.StopPlanOnInteraction,
            TimeCreated = actionPlan.TimeCreated,
            Trigger = dto.Trigger,
            Actions = new List<Action1DTO>()
        };
        foreach (var action in actionPlan.Actions)
        {
            var dtoo = new Action1DTO
            {
                ActionLevel = action.ActionLevel,
                ActionProperties = action.ActionProperties,
                NextActionDelay = action.NextActionDelay,
                TemplateId = action.DataTemplateId
            };
            result.Actions.Add(dtoo);
        }
        return result;
    }
    public async Task<ActionPlanStartDTO> StartLeadActionPlanManually(Guid brokerId, StartActionPlanDTO dto)
    {
        //check lead is not already associated to this actionPlan
        var leads = await _appDbContext.Leads
            .Include(l => l.ActionPlanAssociations.Where(ap => ap.ActionPlanId == dto.ActionPlanID))
            .Where(l => l.BrokerId == brokerId && dto.LeadIds.Contains(l.Id))
            .ToListAsync();

        List<int> failedIDs = new();
        List<int> AlreadyRunningIDs = new();
        foreach (var lead in leads)
        {
            if (lead.ActionPlanAssociations != null && lead.ActionPlanAssociations.Any()) AlreadyRunningIDs.Add(lead.Id);
        }
        if (AlreadyRunningIDs.Count == leads.Count)
        {
            throw new CustomBadRequestException("Workflow Already Associated with provided leads", ProblemDetailsTitles.AlreadyAssociatedToLead);
        }
        leads.RemoveAll(l => AlreadyRunningIDs.Contains(l.Id));
        var timeNow = DateTime.UtcNow;
        var apProjection = await _appDbContext.ActionPlans.
          Select(ap => new
          {
              ap.Id,
              ap.Name,
              ap.StopPlanOnInteraction,
              ap.FirstActionDelay,
              ap.isActive,
              ap.EventsToListenTo, //fow now not used
              firstAction = ap.Actions.Select(fa => new { fa.ActionLevel, fa.Id }).First(a => a.ActionLevel == 1)
          })
          .FirstAsync(app => app.Id == dto.ActionPlanID);
        if (!apProjection.isActive) throw new CustomBadRequestException("Workflow Inactive", ProblemDetailsTitles.ActionPlanInactive);
        var FirstActionDelay = dto.customDelay ?? apProjection.FirstActionDelay;
        var delays = FirstActionDelay?.Split(':');
        TimeSpan timespan = TimeSpan.Zero;
        if (delays != null)
        {
            if (int.TryParse(delays[0], out var days)) timespan += TimeSpan.FromDays(days);
            if (int.TryParse(delays[1], out var hours)) timespan += TimeSpan.FromHours(hours);
            if (int.TryParse(delays[2], out var minutes)) timespan += TimeSpan.FromMinutes(minutes);
        }

        foreach (var lead in leads)
        {
            var actionTracker = new ActionTracker
            {
                TrackedActionId = apProjection.firstAction.Id,
                ActionStatus = ActionStatus.ScheduledToStart,
                HangfireScheduledStartTime = timeNow + timespan,
            };
            var apAssociation = new ActionPlanAssociation
            {
                CustomDelay = dto.customDelay,
                ActionPlanId = dto.ActionPlanID,
                TriggeredManually = true,
                ActionPlanTriggeredAt = timeNow,
                ThisActionPlanStatus = ActionPlanStatus.Running,
                ActionTrackers = new() { actionTracker },
                currentTrackedActionId = apProjection.firstAction.Id,
            };
            lead.ActionPlanAssociations.Add(apAssociation);

            bool OldHasActionPlanToStop = lead.HasActionPlanToStop;
            if (apProjection.StopPlanOnInteraction) lead.HasActionPlanToStop = true;
            if (apProjection.EventsToListenTo != EventType.None)
            {
                lead.EventsForActionPlans |= apProjection.EventsToListenTo; //for now not used
            }
            var APStartedEvent = new AppEvent
            {
                BrokerId = brokerId,
                EventTimeStamp = timeNow,
                EventType = EventType.ActionPlanStarted,
                IsActionPlanResult = true,
                ReadByBroker = true,
                ProcessingStatus = ProcessingStatus.NoNeed,
            };
            APStartedEvent.Props[NotificationJSONKeys.APTriggerType] = NotificationJSONKeys.TriggeredManually;
            APStartedEvent.Props[NotificationJSONKeys.ActionPlanId] = dto.ActionPlanID.ToString();
            APStartedEvent.Props[NotificationJSONKeys.ActionPlanName] = apProjection.Name;
            lead.AppEvents = new() { APStartedEvent };
            string HangfireJobId = "";
            try
            {
                if (delays != null)
                {
                    HangfireJobId = BackgroundJob.Schedule<APProcessor>(p => p.DoActionAsync(lead.Id, apProjection.firstAction.Id, apProjection.firstAction.ActionLevel, dto.ActionPlanID, null), timespan);
                }
                else
                {
                    HangfireJobId = BackgroundJob.Enqueue<APProcessor>(p => p.DoActionAsync(lead.Id, apProjection.firstAction.Id, apProjection.firstAction.ActionLevel, dto.ActionPlanID, null));
                }
                actionTracker.HangfireJobId = HangfireJobId;
            }
            catch (Exception ex)
            {
                _logger.LogError("{tag} Hangfire error scheduling ActionPlan processor" +
                 " for ActionPlan {actionPlanID} and Lead {leadID} with error {error}", TagConstants.HangfireScheduleActionPlan, dto.ActionPlanID, lead.Id, ex.Message + " :" + ex.StackTrace);
                lead.ActionPlanAssociations.Remove(apAssociation);
                lead.AppEvents.Remove(APStartedEvent);
                lead.HasActionPlanToStop = OldHasActionPlanToStop;
                failedIDs.Add(lead.Id);
            }
        }
        //If this fails, ApProcessor's DoActionAsync method verifies if ActionPlanAssociation exists
        //at beginning.
        //But if Hangfire scheduling fails there needs to be a way other than removing everything
        await _appDbContext.SaveChangesAsync();
        bool saved = false;
        byte count = 0;
        var NewQuant = leads.Count;
        while (!saved && count <= 3)
        {
            try
            {
                count++;
                await _appDbContext.ActionPlans.Where(ap => ap.Id == dto.ActionPlanID)
                .ExecuteUpdateAsync(setters =>
                setters.SetProperty(e => e.TimesUsed, e => e.TimesUsed + NewQuant));
                saved = true;
            }
            catch (Exception ex)
            {
                await Task.Delay(300);
            }
        }

        return new ActionPlanStartDTO { AlreadyRunningIDs = AlreadyRunningIDs, errorIDs = failedIDs };
    }


    public async Task DeleteActionPlanAsync(Guid brokerId, int ActionPlanId)
    {
        //delete actions in Hangfire
        //delete actionplanassociations and their action trackers
        //delete ActionPlan and its actions
        var exists = await _appDbContext.ActionPlans.AnyAsync(ap => ap.Id == ActionPlanId && ap.BrokerId == brokerId);
        if (!exists) throw new CustomBadRequestException("ActionPlan Not Found", ProblemDetailsTitles.NotFound, 404);
        var ActionPlanAssociations = await _appDbContext.ActionPlanAssociations
          .Where(apa => apa.ActionPlanId == ActionPlanId && apa.ActionPlan.BrokerId == brokerId)
          .Include(apa => apa.ActionTrackers.Where(a => a.ActionStatus == ActionStatus.ScheduledToStart || a.ActionStatus == ActionStatus.Failed))
          .AsNoTracking()
          .ToListAsync();

        foreach (var apass in ActionPlanAssociations)
        {
            if (apass.ActionTrackers.Any())
            {
                foreach (var ta in apass.ActionTrackers)
                {
                    var jobId = ta.HangfireJobId;
                    if (jobId != null)
                        try
                        {
                            BackgroundJob.Delete(jobId);
                        }
                        catch (Exception) { }
                }
            }
        }
        //this will delete action trackers  and Actions also
        await _appDbContext.Database.ExecuteSqlRawAsync($"DELETE FROM \"ActionPlanAssociations\" WHERE \"ActionPlanId\" = {ActionPlanId};"
          + $"DELETE FROM \"ActionPlans\" WHERE \"Id\" = {ActionPlanId};");
    }
}
