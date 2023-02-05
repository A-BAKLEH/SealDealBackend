using Core.Constants.ProblemDetailsTitles;
using Core.Domain.ActionPlanAggregate;
using Core.Domain.ActionPlanAggregate.Actions;
using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Hangfire.Server;
using Humanizer;
using Infrastructure.Data;
using Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Exceptions;
using Web.ApiModels.APIResponses.ActionPlans;
using Web.ApiModels.RequestDTOs.ActionPlans;
using Web.Constants;
using Web.Outbox;
using Web.Outbox.Config;
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

  public async Task<ActionPlanDTO> CreateActionPlanAsync(CreateActionPlanDTO dto, Guid brokerId)
  {
    NotifType trigger;
    switch (dto.Trigger)
    {
      case "LeadAssigned":
        trigger = NotifType.LeadAssigned;
        break;
      case "Manual":
        trigger = NotifType.None;
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
      ActionsCount = dto.Actions.Count,
      Name = dto.Name,
      StopPlanOnInteraction = dto.StopPlanOnInteraction,
      FirstActionDelay = dto.FirstActionDelay,
      Actions = new()
    };

    if (dto.StopPlanOnInteraction)
    {
      //TODO re check these notifs especially EmailEvent
      actionPlan.NotifsToListenTo = NotifType.EmailEvent | NotifType.SmsEvent | NotifType.CallMissed | NotifType.CallEvent;
    }

    foreach (var actionDTO in dto.Actions)
    {
      var type = actionDTO.ActionType;
      ActionBase actionBase;
      switch (type)
      {
        case "SendEmail":
          actionBase = new SendEmail();
          actionBase.ActionProperties[SendEmail.EmailTemplateId] = actionDTO.Properties[SendEmail.EmailTemplateId];
          break;
        case "ChangeLeadStatus":
          actionBase = new ChangeLeadStatus();
          var InputStatus = actionDTO.Properties[ChangeLeadStatus.NewLeadStatus];
          var StatusExists = Enum.TryParse<LeadStatus>(InputStatus, true, out var NewLeadStatus);
          if (!StatusExists) throw new CustomBadRequestException($"invalid Lead Status '{InputStatus}'", ProblemDetailsTitles.InvalidInput);
          actionBase.ActionProperties[ChangeLeadStatus.NewLeadStatus] = NewLeadStatus.ToString();
          break;
        case "SendSms":
          actionBase = new SendSms { };
          actionBase.ActionProperties[SendSms.SmsTemplateId] = actionDTO.Properties[SendSms.SmsTemplateId];
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
    if (actionPlan.Triggers != NotifType.None)
    {
      var broker = _appDbContext.Brokers.First(b => b.Id == brokerId);
      if (!broker.NotifsForActionPlans.HasFlag(actionPlan.Triggers))
      {
        broker.NotifsForActionPlans |= actionPlan.Triggers;
      }
    }

    _appDbContext.ActionPlans.Add(actionPlan);
    await _appDbContext.SaveChangesAsync();

    var result = new ActionPlanDTO
    {
      ActionsCount = actionPlan.ActionsCount,
      FirstActionDelay = actionPlan.FirstActionDelay,
      isActive = actionPlan.isActive,
      id = actionPlan.Id,
      name = actionPlan.Name,
      StopPlanOnInteraction = actionPlan.StopPlanOnInteraction,
      TimeCreated = actionPlan.TimeCreated.UtcDateTime,
      Trigger = dto.Trigger,
      Actions = new List<ActionDTO>()
    };
    foreach (var action in actionPlan.Actions)
    {
      result.Actions.Add(new ActionDTO
      {
        ActionLevel = action.ActionLevel,
        ActionProperties = action.ActionProperties,
        NextActionDelay = action.NextActionDelay,
      });
    }
    return result;
  }
  public async Task StartLeadActionPlanManually(Guid brokerId, int LeadId, int ActionPlanId, string? customDelay = null)
  {
    //check lead is not already associated to this actionPlan
    var APAssExists = await _appDbContext.ActionPlanAssociations
      .AnyAsync(apass => apass.ActionPlanId == ActionPlanId && apass.LeadId == LeadId);
    if (APAssExists)
    {
      throw new CustomBadRequestException("Action Plan Already Associated", ProblemDetailsTitles.AlreadyAssociatedToLead);
    }

    var timeNow = DateTime.UtcNow;
    //create actionPlanAssociation associated to Lead
    var apProjection = await _appDbContext.ActionPlans.
      Select(ap => new
      {
        ap.Id,
        ap.StopPlanOnInteraction,
        ap.FirstActionDelay,
        ap.NotifsToListenTo,
        firstAction = ap.Actions.Select(fa => new { fa.ActionLevel, fa.Id }).First(a => a.ActionLevel == 1)
      })
      .FirstAsync(app => app.Id == ActionPlanId);

    var FirstActionDelay = customDelay ?? apProjection.FirstActionDelay;
    var delays = FirstActionDelay?.Split(':');
    string HangfireJobId = "";

    TimeSpan timespan = TimeSpan.Zero;
    if (delays != null)
    {
      if (int.TryParse(delays[0], out var days)) timespan += TimeSpan.FromDays(days);
      if (int.TryParse(delays[1], out var hours)) timespan += TimeSpan.FromHours(hours);
      if (int.TryParse(delays[2], out var minutes)) timespan += TimeSpan.FromMinutes(minutes);
    }

    var actionTracker = new ActionTracker
    {
      TrackedActionId = apProjection.firstAction.Id,
      ActionStatus = ActionStatus.ScheduledToStart,
      HangfireScheduledStartTime = timeNow + timespan,
    };
    var apAssociation = new ActionPlanAssociation
    {
      CustomDelay = customDelay,
      ActionPlanId = ActionPlanId,
      TriggerNotificationId = null, //cuz triggered manually
      LeadId = LeadId,
      ActionPlanTriggeredAt = timeNow,
      ThisActionPlanStatus = ActionPlanStatus.Running,
      ActionTrackers = new() { actionTracker },
      currentTrackedActionId = apProjection.firstAction.Id,
    };

    //assign Lead's NotifsToListenTo if StopPlanOnInteraction is True
    if (apProjection.StopPlanOnInteraction)
    {
      var lead = await _appDbContext.Leads.FirstAsync(l => l.Id == LeadId);
      var StopNotifs = NotifType.EmailEvent | NotifType.SmsEvent | NotifType.CallMissed | NotifType.CallEvent;
      if (!lead.NotifsForActionPlans.HasFlag(StopNotifs))
      {
        lead.NotifsForActionPlans |= StopNotifs;
      }
    }
    var notif = new Notification
    {
      BrokerId = brokerId,
      LeadId = LeadId,
      EventTimeStamp = timeNow,
      NotifType = NotifType.ActionPlanStarted,
      ReadByBroker = true,
      NotifyBroker = false,
      ProcessingStatus = ProcessingStatus.NoNeed,
    };
    notif.NotifProps[NotificationJSONKeys.APTriggerType] = NotificationJSONKeys.TriggeredManually;
    notif.NotifProps[NotificationJSONKeys.ActionPlanId] = ActionPlanId.ToString();
    _appDbContext.Notifications.Add(notif);

    if (delays != null)
    {
      HangfireJobId = Hangfire.BackgroundJob.Schedule<APProcessor>(p => p.DoActionAsync(LeadId, apProjection.firstAction.Id, apProjection.firstAction.ActionLevel, ActionPlanId), timespan);
    }
    else
    {
      HangfireJobId = Hangfire.BackgroundJob.Enqueue<APProcessor>(p => p.DoActionAsync(LeadId, apProjection.firstAction.Id, apProjection.firstAction.ActionLevel, ActionPlanId));
    }
    actionTracker.HangfireJobId = HangfireJobId;

    await _appDbContext.SaveChangesAsync();
  }
}
