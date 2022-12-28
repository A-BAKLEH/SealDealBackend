using System.Reflection;
using Clean.Architecture.Core.Domain.ActionPlanAggregate;
using Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions;
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate.EmailConnection;
using Clean.Architecture.Core.Domain.BrokerAggregate.Templates;
using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.Core.Domain.LeadAggregate.Interactions;
using Clean.Architecture.Core.Domain.NotificationAggregate;
using Clean.Architecture.Core.Domain.TasksAggregate;
using Clean.Architecture.Core.Domain.TestAggregate;
using Clean.Architecture.SharedKernel.DomainNotifications;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Infrastructure.Data;

public class AppDbContext : DbContext
{

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
