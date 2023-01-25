using Core.Constants.ProblemDetailsTitles;
using Core.Domain.ActionPlanAggregate;
using Core.Domain.ActionPlanAggregate.Actions;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Infrastructure.Data;
using SharedKernel.Exceptions;
using Web.ApiModels.APIResponses.ActionPlans;
using Web.ApiModels.RequestDTOs.ActionPlans;
using Web.Outbox;

namespace Web.ControllerServices.QuickServices;

public class ActionPQService
{
  private readonly AppDbContext _appDbContext;
  private readonly ILogger<ActionPQService> _logger;  
  public ActionPQService(ILogger<ActionPQService> logger,AppDbContext appDbContext)
  {
    _logger = logger;
    _appDbContext = appDbContext;
  }

  public async Task<ActionPlanDTO> CreateActionPlanAsync(CreateActionPlanDTO dto,Guid brokerId)
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

    if(dto.FirstActionDelay != null)
    {
      var delays = dto.FirstActionDelay.Trim().Split(':');
      int lol = 0;
      if (delays.Count() != 3 || !int.TryParse(delays[0], out lol) || !int.TryParse(delays[1], out lol) || !int.TryParse(delays[2], out lol))
      {
        throw new CustomBadRequestException($"input {dto.FirstActionDelay}",ProblemDetailsTitles.InvalidInput);
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

    if(dto.StopPlanOnInteraction)
    {
      //TODO re check these notifs especially EmailEvent
      actionPlan.NotifsToListenTo = NotifType.EmailEvent | NotifType.SmsEvent | NotifType.CallMissed;
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
          actionBase= new SendSms { };
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

    if(actionPlan.Triggers != NotifType.None)
    {
      var broker = _appDbContext.Brokers.First(b => b.Id == brokerId);
      if(!broker.NotifsForActionPlans.HasFlag(actionPlan.Triggers))
      { 
        broker.NotifsForActionPlans |= actionPlan.Triggers;
      }
    }

    _appDbContext.ActionPlans.Add(actionPlan);
    await _appDbContext.SaveChangesAsync();

    var result = new ActionPlanDTO
    {
      ActionsCount = actionPlan.ActionsCount,
      FirstActionDelay= actionPlan.FirstActionDelay,
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
        ActionProperties= action.ActionProperties,
        NextActionDelay= action.NextActionDelay,
      });
    }
    return result;

  }
  
}
