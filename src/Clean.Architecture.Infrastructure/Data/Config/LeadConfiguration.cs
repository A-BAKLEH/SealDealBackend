using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.LeadAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;
public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
  public void Configure(EntityTypeBuilder<Lead> builder)
  {
    builder.Property(b => b.LeadStatus).HasConversion<string>();

    builder
        .HasMany(l => l.Tags)
        .WithMany(t => t.Leads)
        .UsingEntity<Dictionary<string, object>>(
            "LeadTag",
            j => j.HasOne<Tag>().WithMany().OnDelete(DeleteBehavior.ClientCascade),
            j => j.HasOne<Lead>().WithMany().OnDelete(DeleteBehavior.Cascade));
  }
}
