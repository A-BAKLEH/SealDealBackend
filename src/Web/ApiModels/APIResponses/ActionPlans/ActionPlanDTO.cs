﻿using Core.Domain.NotificationAggregate;

namespace Web.ApiModels.APIResponses.ActionPlans;

public class ActionPlanDTO
{
  public int id { get; set; }
  public string name { get; set; }
  public Guid brokerId { get; set; }
  public bool isActive { get; set; }
  public NotifType FlagTrigger { get; set; }
  public List<string> Triggers { get; set; }
  public bool StopPlanOnInteraction { get; set; }
  public string? FirstActionDelay { get; set; }
  public DateTime TimeCreated { get; set; }
  public int ActionsCount { get; set; }
  public int ActiveOnXLeads { get; set; }
  public IEnumerable<ActionDTO> Actions { get; set; }
}

public class ActionDTO
{
  public int ActionLevel { get; set; }
  public Dictionary<string, string> ActionProperties { get; set; }
  public string? NextActionDelay { get; set; }
}
