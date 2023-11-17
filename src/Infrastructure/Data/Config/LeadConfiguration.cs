using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Infrastructure.Data.Config;
public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.Property(b => b.LeadStatus).HasConversion<string>();
        //builder.HasIndex(b => b.Email);
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

        builder.HasMany(x => x.AINurturings)
            .WithOne(b => b.lead)
            .HasForeignKey(b => b.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(l => l.LeadFirstName).HasMaxLength(50);
        builder.Property(l => l.LeadLastName).HasMaxLength(50);
        builder.Property(a => a.PhoneNumber).HasMaxLength(30);
        //builder.Property(a => a.Email).HasMaxLength(100);
    }
}
