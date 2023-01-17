using System.Reflection;
using Core.Domain.ActionPlanAggregate;
using Core.Domain.ActionPlanAggregate.Actions;
using Core.Domain.AgencyAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.BrokerAggregate.EmailConnection;
using Core.Domain.BrokerAggregate.Templates;
using Core.Domain.LeadAggregate;
using Core.Domain.LeadAggregate.Interactions;
using Core.Domain.NotificationAggregate;
using Core.Domain.TasksAggregate;
using Core.Domain.TestAggregate;
using SharedKernel.DomainNotifications;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
  public bool OnlyOutboxEvents { get; set; } = false;
  public AppDbContext(DbContextOptions<AppDbContext> options)
      : base(options)
  {
  }

  //test
  public DbSet<TestBase> TestBase => Set<TestBase>();
  public DbSet<TestEntity1> TestEntity1 => Set<TestEntity1>();
  public DbSet<TestEntity2> TestEntity2 => Set<TestEntity2>();

  //---------
  public DbSet<Agency> Agencies => Set<Agency>();
  public DbSet<Listing> Listings => Set<Listing>();
  public DbSet<BrokerListingAssignment> BrokerListingAssignments => Set<BrokerListingAssignment>();
  public DbSet<Area> Areas => Set<Area>();
  public DbSet<Broker> Brokers => Set<Broker>();
  public DbSet<FolderSync> FolderSyncs => Set<FolderSync>();
  public DbSet<ConnectedEmail> ConnectedEmails => Set<ConnectedEmail>();
  public DbSet<Template> Templates => Set<Template>();
  public DbSet<SmsTemplate> SmsTemplates => Set<SmsTemplate>();
  public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
  public DbSet<Tag> Tags => Set<Tag>();
  public DbSet<ToDoTask> ToDoTasks=> Set<ToDoTask>();

  public DbSet<Lead> Leads => Set<Lead>();
  public DbSet<LeadInteraction> LeadInteractions => Set<LeadInteraction>();
  public DbSet<EmailInteraction> EmailInteractions => Set<EmailInteraction>();
  public DbSet<SmsInteraction> SmsInteractions => Set<SmsInteraction>();
  public DbSet<CallInteraction> CallInteractions => Set<CallInteraction>();


  public DbSet<Note> Notes => Set<Note>();

  //Action Plans  + Notifications---------------
  public DbSet<ActionBase> Actions => Set<ActionBase>();
  public DbSet<ChangeLeadStatusAction> ChangeLeadStatusActions => Set<ChangeLeadStatusAction>();
  public DbSet<SendEmailAction> SendEmailActions => Set<SendEmailAction>();
  
  public DbSet<RecurrentTaskBase> RecurrentTasks => Set<RecurrentTaskBase>();
  public DbSet<FetchEmailsTask> FetchEmailsTasks => Set<FetchEmailsTask>();
  public DbSet<FetchSmsTask> FetchSmsTasks => Set<FetchSmsTask>();
  public DbSet<OutboxDictsTask> OutboxDictsTasks=> Set<OutboxDictsTask>();

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
