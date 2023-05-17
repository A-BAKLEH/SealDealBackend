using Core.Constants.ProblemDetailsTitles;
using Core.Domain.ActionPlanAggregate;
using Core.Domain.ActionPlanAggregate.Actions;
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
              TimeCreated = a.TimeCreated.UtcDateTime,
              FlagTrigger = a.Triggers,
              Actions = a.Actions.OrderBy(a => a.ActionLevel).Select(aa => new ActionDTO
              {
                  ActionLevel = aa.ActionLevel,
                  ActionProperties = aa.ActionProperties,
                  NextActionDelay = aa.NextActionDelay,
                  TemplateId = aa.DataId
              }),
              ActiveOnXLeads = a.ActionPlanAssociations.Where(apa => apa.ThisActionPlanStatus == ActionPlanStatus.Running).Count(),
              leads = a.ActionPlanAssociations.Select(apa => new LeadNameIdDTO { LeadId = apa.LeadId, firstName = apa.lead.LeadFirstName, lastName = apa.lead.LeadLastName })
          })
          .OrderByDescending(a => a.ActiveOnXLeads)
          .ToListAsync();

        foreach (var item in actionPlans)
        {
            //TECH
            item.Triggers = item.FlagTrigger.GetIndividualFlags().Select(f => f.ToString()).ToList();
        }
        return actionPlans;
    }
    public async Task<ActionPlan1DTO> CreateActionPlanAsync(CreateActionPlanDTO dto, Guid brokerId)
    {
        EventType trigger;
        switch (dto.Trigger)
        {
            case "LeadAssigned":
                trigger = EventType.LeadAssigned;
                break;
            case "Manual":
                trigger = EventType.None;
                break;
            default:
                throw new CustomBadRequestException("invalid trigger", ProblemDetailsTitles.InvalidInput);
        };
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

        //for now EventsToListenTo is None cuz StopPlanOnInteraction covers the interactions

        foreach (var actionDTO in dto.Actions)
        {
            var type = actionDTO.ActionType;
            ActionBase actionBase;
            switch (type)
            {
                case "SendEmail":
                    if (actionDTO.TemplateId == null) throw new CustomBadRequestException("TemplateId empty", ProblemDetailsTitles.InvalidInput);
                    actionBase = new SendEmail
                    {
                        DataId = actionDTO.TemplateId
                    };
                    break;
                case "ChangeLeadStatus":
                    actionBase = new ChangeLeadStatus();
                    var InputStatus = actionDTO.Properties[ChangeLeadStatus.NewLeadStatus];
                    var StatusExists = Enum.TryParse<LeadStatus>(InputStatus, true, out var NewLeadStatus);
                    if (!StatusExists) throw new CustomBadRequestException($"invalid Lead Status '{InputStatus}'", ProblemDetailsTitles.InvalidInput);
                    actionBase.ActionProperties[ChangeLeadStatus.NewLeadStatus] = NewLeadStatus.ToString();
                    break;
                case "SendSms":
                    if (actionDTO.TemplateId == null) throw new CustomBadRequestException("TemplateId empty", ProblemDetailsTitles.InvalidInput);
                    actionBase = new SendSms
                    {
                        DataId = actionDTO.TemplateId
                    };
                    break;
                default:
                    throw new CustomBadRequestException($"Invalid action {actionDTO.ActionType}", ProblemDetailsTitles.InvalidInput);
            }

            var delays = actionDTO.NextActionDelay.Trim().Split(':');
            int lol = 0;
            if (delays.Count() != 3 || !int.TryParse(delays[0], out lol) || !int.TryParse(delays[1], out lol) || !int.TryParse(delays[2], out lol))
            {
                throw new CustomBadRequestException($"input {dto.FirstActionDelay}", ProblemDetailsTitles.InvalidInput);
            }

            actionBase.ActionLevel = (byte)actionDTO.ActionLevel;
            actionBase.NextActionDelay = actionDTO.NextActionDelay;
            actionPlan.Actions.Add(actionBase);
        }

        //if action plan has an automatic trigger, add it to broker's NotifsForActionPlans
        if (actionPlan.Triggers != EventType.None)
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
            TimeCreated = actionPlan.TimeCreated.UtcDateTime,
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
                TemplateId = action.DataId
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
            throw new CustomBadRequestException("Action Plan Already Associated with provided leads", ProblemDetailsTitles.AlreadyAssociatedToLead);
        }
        leads.RemoveAll(l => AlreadyRunningIDs.Contains(l.Id));
        var timeNow = DateTime.UtcNow;
        var apProjection = await _appDbContext.ActionPlans.
          Select(ap => new
          {
              ap.Id,
              ap.StopPlanOnInteraction,
              ap.FirstActionDelay,
              ap.EventsToListenTo,
              firstAction = ap.Actions.Select(fa => new { fa.ActionLevel, fa.Id }).First(a => a.ActionLevel == 1)
          })
          .FirstAsync(app => app.Id == dto.ActionPlanID);

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
                TriggerNotificationId = null, //cuz triggered manually
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
                lead.EventsForActionPlans |= apProjection.EventsToListenTo;
            }
            var APStartedEvent = new AppEvent
            {
                BrokerId = brokerId,
                EventTimeStamp = timeNow,
                EventType = EventType.ActionPlanStarted,
                IsActionPlanResult = true,
                ReadByBroker = true,
                NotifyBroker = false,
                ProcessingStatus = ProcessingStatus.NoNeed,
            };
            APStartedEvent.Props[NotificationJSONKeys.APTriggerType] = NotificationJSONKeys.TriggeredManually;
            APStartedEvent.Props[NotificationJSONKeys.ActionPlanId] = dto.ActionPlanID.ToString();
            lead.AppEvents = new() { APStartedEvent };
            string HangfireJobId = "";
            try
            {
                if (delays != null)
                {
                    HangfireJobId = BackgroundJob.Schedule<APProcessor>(p => p.DoActionAsync(lead.Id, apProjection.firstAction.Id, apProjection.firstAction.ActionLevel, dto.ActionPlanID), timespan);
                }
                else
                {
                    HangfireJobId = BackgroundJob.Enqueue<APProcessor>(p => p.DoActionAsync(lead.Id, apProjection.firstAction.Id, apProjection.firstAction.ActionLevel, dto.ActionPlanID));
                }
            }
            catch(Exception ex)
            {
                _logger.LogCritical("{place} Hangfire error scheduling ActionPlan processor" +
                 " for ActionPlan {ActionPlanID} and Lead {LeadID} with error {Error}", "ScheduleActionPlanProcessor",dto.ActionPlanID,lead.Id, ex.Message);
                lead.ActionPlanAssociations.Remove(apAssociation);
                lead.AppEvents.Remove(APStartedEvent);
                lead.HasActionPlanToStop = OldHasActionPlanToStop;
                failedIDs.Add(lead.Id);
            }
            actionTracker.HangfireJobId = HangfireJobId;
        }
        //If this fails, ApProcessor's DoActionAsync method verifies if ActionPlanAssociation exists
        //at beginning.
        //But if Hangfire scheduling fails there needs to be a way other than removing everything
        await _appDbContext.SaveChangesAsync();
        return new ActionPlanStartDTO { AlreadyRunningIDs = AlreadyRunningIDs, errorIDs = failedIDs};
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
          .AsSplitQuery()
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
        await _appDbContext.Database.ExecuteSqlRawAsync($"DELETE FROM ActionPlanAssociations WHERE ActionPlanId = {ActionPlanId};"
          + $"DELETE FROM ActionPlans WHERE Id = {ActionPlanId};");

    }
}
