using System.Reflection;
using Clean.Architecture.Core.AgencyAggregate;
using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.Core.LeadAggregate;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Infrastructure.Data;

public class AppDbContext : DbContext
{
  private readonly IDomainEventDispatcher? _dispatcher;

  public AppDbContext(DbContextOptions<AppDbContext> options,
    IDomainEventDispatcher? dispatcher)
      : base(options)
  {
    _dispatcher = dispatcher;
  }

 // public DbSet<ToDoItem> ToDoItems => Set<ToDoItem>();
  //public DbSet<Project> Projects => Set<Project>();
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

  public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
  {
    int result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    // ignore events if no dispatcher provided
    if (_dispatcher == null) return result;

    // dispatch events only if save was successful
    var entitiesWithEvents = ChangeTracker.Entries<EntityBase>()
        .Select(e => e.Entity)
        .Where(e => e.DomainEvents.Any())
        .ToArray();

    await _dispatcher.DispatchAndClearEvents(entitiesWithEvents);

    return result;
  }

  public override int SaveChanges()
  {
    return SaveChangesAsync().GetAwaiter().GetResult();
  }
}
