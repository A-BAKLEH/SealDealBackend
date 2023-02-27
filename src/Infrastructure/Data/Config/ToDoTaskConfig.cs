using Core.Domain.BrokerAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;
public class ToDoTaskConfig : IEntityTypeConfiguration<ToDoTask>
{
  public void Configure(EntityTypeBuilder<ToDoTask> builder)
  {
    builder.
    HasOne(t => t.Lead)
   .WithMany(e => e.ToDoTasks)
   .IsRequired(false)
   .OnDelete(DeleteBehavior.ClientCascade);
  }
}
