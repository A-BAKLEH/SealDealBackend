using Core.Constants.ProblemDetailsTitles;
using Core.Domain.ActionPlanAggregate;
using Core.Domain.ActionPlanAggregate.Actions;
using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Humanizer;
using Infrastructure.Data;
using Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Exceptions;
using Web.ApiModels.APIResponses.ActionPlans;
using Web.ApiModels.RequestDTOs.ActionPlans;
using Web.Outbox;
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

      actionBase.ActionLevel = actionDTO.ActionLevel;
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
  public async Task StartLeadActionPlanManually(Broker broker, int LeadId, int ActionPlanId)
  {
    //params to add
    string? customDelay = null;
    //

    using var transaction = _appDbContext.Database.BeginTransaction();
    //check lead is not already associated to actionPlan
    var existingAP = _appDbContext.ActionPlanAssociations.FirstOrDefault(apass => apass.ActionPlanId == ActionPlanId);
    if (existingAP != null)
    {
      throw new CustomBadRequestException("Action Plan Already Associated", ProblemDetailsTitles.AlreadyAssociatedToLead);
    }
    //create actionPlanAssociation associated to Lead
    var apProjection = _appDbContext.ActionPlans.
      Select(ap => new
      {
        ap.Id,
        ap.StopPlanOnInteraction,
        ap.FirstActionDelay,
        ap.NotifsToListenTo,
        firstAction = ap.Actions.First(a => a.Id == ActionPlanId)
      })
      .First(app => app.Id == ActionPlanId);

    var FirstActionDelay = customDelay ?? apProjection.FirstActionDelay;
    var delays = FirstActionDelay?.Split(':');
    string HangfireJobId = "";
    //TODO IMPORTANT ACTION ID TO PASS TO HANGFIRE
    if(delays != null)
    {
      var timespan = TimeSpan.Zero;
      if (int.TryParse(delays[0], out var days)) timespan += TimeSpan.FromDays(days);
      if (int.TryParse(delays[1], out var hours)) timespan+= TimeSpan.FromHours(hours);
      if (int.TryParse(delays[2], out var minutes)) timespan+= TimeSpan.FromMinutes(minutes);

      HangfireJobId = Hangfire.BackgroundJob.Schedule<APProcessor>(p => p.DoActionAsync(1),timespan);
    }
    else
    {
      HangfireJobId = Hangfire.BackgroundJob.Enqueue<APProcessor>(p => p.DoActionAsync(1));
    }

    
    var actionTracker = new ActionTracker
    {
      TrackedActionId = apProjection.firstAction.Id,
      ActionStatus = ActionStatus.ScheduledToStart,
      HangfireJobId = HangfireJobId
    };
    var apAssociation = new ActionPlanAssociation
    {
      CustomDelay = customDelay,
      ActionPlanId = ActionPlanId,
      TriggerNotificationId = null, //cuz triggered manually
      LeadId = LeadId,
      ActionPlanTriggeredAt = DateTime.UtcNow,
      ThisActionPlanStatus = ActionPlanStatus.Running,
      ActionTrackers = new() { actionTracker },
      currentTrackedActionId = apProjection.firstAction.Id,

      //FirstActionHangfireId FOR NOW IGNORE, PROBABLY TO BE DELETED
    };

    if(apProjection.StopPlanOnInteraction)

    transaction.Commit();
  /// <summary>
  /// hangfire job that will execute THIS tracked action, not the one after the delay
  /// </summary>
  /*public string? HangfireJobId { get; set; }
  /// <summary>
  /// if null immediate
  /// </summary>
  public DateTimeOffset? HangfireScheduledStartTime { get; set; }
  public DateTimeOffset? ExecutionCompletedTime { get; set; }
  /// <summary>
  /// Details about failures or other relevant Status Info
  /// </summary>
  public string? ActionStatusInfo { get; set; }
  /// <summary>
  /// can be the sent emailId, sent smsId, Some action result that should be tracked
  /// </summary>
  public int? ActionResultId { get; set; }*/




  //change lead NotifsTOListenTo if necessary ,fow now this is only
  //if stopplanoninteraction == true

  //create notif ActionPlan Started
  }
}
