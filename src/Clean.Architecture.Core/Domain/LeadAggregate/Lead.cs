﻿
using Clean.Architecture.Core.Domain.ActionPlanAggregate;
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.NotificationAggregate;
using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.LeadAggregate;

public enum LeadStatus
{
  New, Active, Client, Closed, Dead
}

public class Lead : Entity<int>
{
  public int AgencyId { get; set; }
  public Agency Agency { get; set; }
  public string LeadFirstName { get; set; }
  public string? LeadLastName { get; set; }
  public string? PhoneNumber { get; set; }
  public string? Email { get; set; }
  public int? Budget { get; set; }
  public DateTime EntryDate { get; set; } = DateTime.UtcNow;
  public LeadStatus LeadStatus { get; set; } = LeadStatus.New;
  public Broker? Broker { get; set; }
  public Guid? BrokerId { get; set; }
  public List<Area>? AreasOfInterest { get; set; }
  public List<LeadListing>? ListingsOfInterest { get; set; }
  public List<Note>? Notes { get; set; }
  public List<Tag>? Tags { get; set; }
  public List<ActionPlanAssociation>? ActionPlanAssociations { get; set; }
  public List<Notification>? LeadHistoryEvents { get; set; }
}

