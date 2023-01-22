using System.Text.Json;
using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;
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

    var options = new JsonSerializerOptions(JsonSerializerDefaults.General);
    builder
        .Property(x => x.SourceDetails)
        .HasConversion(
            v => JsonSerializer.Serialize(v, options),
            s => JsonSerializer.Deserialize<Dictionary<string, string>>(s, options)!,
            ValueComparer.CreateDefault(typeof(Dictionary<string, string>), true)
        );
  }
}
