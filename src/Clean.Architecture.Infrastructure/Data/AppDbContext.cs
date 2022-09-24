using System.Reflection;
using Clean.Architecture.Core.Config;
using Clean.Architecture.Core.Domain.ActionPlanAggregate;
using Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions;
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.Core.Domain.NotificationAggregate;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Infrastructure.Data;

public class AppDbContext : DbContext
{

  public AppDbContext(DbContextOptions<AppDbContext> options)
      : base(options)
  {
  }
  public DbSet<Agency> Agencies => Set<Agency>();
  public DbSet<Listing> Listings => Set<Listing>();
  public DbSet<LeadListing> LeadListing => Set<LeadListing>();
  public DbSet<Area> Areas => Set<Area>();
  public DbSet<Broker> Brokers => Set<Broker>();
  public DbSet<SmsTemplate> SmsTemplates => Set<SmsTemplate>();
  public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
  public DbSet<Tag> Tags => Set<Tag>();
  public DbSet<ToDoTask> ToDoTasks=> Set<ToDoTask>();

  public DbSet<Lead> Leads => Set<Lead>();
  public DbSet<Note> Notes => Set<Note>();

  //Action Plans  + Notifications---------------
  public DbSet<ActionBase> Actions => Set<ActionBase>();
  public DbSet<ChangeLeadStatusAction> ChangeLeadStatusActions => Set<ChangeLeadStatusAction>();
  public DbSet<SendEmailAction> SendEmailActions => Set<SendEmailAction>();
  

  public DbSet<ActionPlan> ActionPlans => Set<ActionPlan>();
  public DbSet<ActionPlanAssociation> ActionPlanAssociations => Set<ActionPlanAssociation>();

  public DbSet<ActionTracker> ActionTrackers => Set<ActionTracker>();
  public DbSet<Notification> Notifications => Set<Notification>();
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
  }

  private List<IDomainEventNotification<IDomainEvent>> _domainEventNotifications;

  public IReadOnlyCollection<IDomainEventNotification<IDomainEvent>> DomainEventNotifications => _domainEventNotifications?.AsReadOnly();

  public void AddDomainEventNotification(IDomainEventNotification<IDomainEvent> domainEventNotification)
  {
    _domainEventNotifications = _domainEventNotifications ?? new List<IDomainEventNotification<IDomainEvent>>();
    this._domainEventNotifications.Add(domainEventNotification);
  }

  public void ClearDomainEventNotifications()
  {
    _domainEventNotifications.Clear();
  }

}
