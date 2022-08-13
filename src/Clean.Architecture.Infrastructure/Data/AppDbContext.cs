using System.Reflection;
using Clean.Architecture.Core.AgencyAggregate;
using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.Core.Config;
using Clean.Architecture.Core.LeadAggregate;
using Clean.Architecture.SharedKernel;
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
  public DbSet<Area> Areas => Set<Area>();
  public DbSet<Broker> Brokers => Set<Broker>();
  public DbSet<SmsTemplate> SmsTemplates => Set<SmsTemplate>();
  public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
  public DbSet<Tag> Tags => Set<Tag>();
  public DbSet<ToDoTask> ToDoTasks=> Set<ToDoTask>();

  public DbSet<Lead> Leads => Set<Lead>();
  public DbSet<Note> Notes => Set<Note>();
  public DbSet<History> Histories => Set<History>();


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
