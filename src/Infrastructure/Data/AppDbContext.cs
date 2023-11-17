using Core.Domain.ActionPlanAggregate;
using Core.Domain.AgencyAggregate;
using Core.Domain.AINurturingAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.BrokerAggregate.EmailConnection;
using Core.Domain.BrokerAggregate.Templates;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.Domain.TasksAggregate;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<BrokerListingAssignment> BrokerListingAssignments => Set<BrokerListingAssignment>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Broker> Brokers => Set<Broker>();
    public DbSet<ConnectedEmail> ConnectedEmails => Set<ConnectedEmail>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<SmsTemplate> SmsTemplates => Set<SmsTemplate>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ToDoTask> ToDoTasks => Set<ToDoTask>();

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadEmail> LeadEmails => Set<LeadEmail>();
    public DbSet<Note> Notes => Set<Note>();

    //Action Plans  + Notifications---------------
    public DbSet<ActionPlanAction> Actions => Set<ActionPlanAction>();
    public DbSet<RecurrentTaskBase> RecurrentTasks => Set<RecurrentTaskBase>();
    public DbSet<OutboxDictsTask> OutboxDictsTasks => Set<OutboxDictsTask>();
    public DbSet<BrokerNotifAnalyzerTask> BrokerNotifAnalyzerTasks => Set<BrokerNotifAnalyzerTask>();
    public DbSet<BrokerCleanupTask> BrokerCleanupTasks => Set<BrokerCleanupTask>();
    public DbSet<ActionPlan> ActionPlans => Set<ActionPlan>();
    public DbSet<ActionPlanAssociation> ActionPlanAssociations => Set<ActionPlanAssociation>();
    public DbSet<AINurturing> AINurturings => Set<AINurturing>();

    public DbSet<ActionTracker> ActionTrackers => Set<ActionTracker>();
    public DbSet<Notif> Notifs => Set<Notif>();
    public DbSet<AppEvent> AppEvents => Set<AppEvent>();
    public DbSet<EmailEvent> EmailEvents => Set<EmailEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
