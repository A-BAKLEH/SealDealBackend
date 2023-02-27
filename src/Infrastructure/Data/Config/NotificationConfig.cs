using System.Text.Json;
using Core.Domain.NotificationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;
public class NotificationConfig : IEntityTypeConfiguration<Notification>
{
  public void Configure(EntityTypeBuilder<Notification> builder)
  {
    var options = new JsonSerializerOptions(JsonSerializerDefaults.General);

    builder
        .Property(x => x.NotifProps)
        .HasConversion(
            v => JsonSerializer.Serialize(v, options),
            s => JsonSerializer.Deserialize<Dictionary<string, string>>(s, options)!,
            ValueComparer.CreateDefault(typeof(Dictionary<string, string>), true)
        );

    builder.
      HasOne(x => x.lead)
      .WithMany(l => l.LeadHistoryEvents)
      .IsRequired(false)
      .OnDelete(DeleteBehavior.ClientCascade);
  }
}
