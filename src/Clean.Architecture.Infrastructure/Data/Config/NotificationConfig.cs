using System.Text.Json;
using Clean.Architecture.Core.Domain.NotificationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;
public class NotificationConfig : IEntityTypeConfiguration<Notification>
{
  public void Configure(EntityTypeBuilder<Notification> builder)
  {
    var options = new JsonSerializerOptions(JsonSerializerDefaults.General);

    builder
        .Property(x => x.NotifData)
        //.HasColumnName("Values")
        //.HasColumnType("BLOB")
        .HasConversion(
            v => JsonSerializer.Serialize(v, options),
            s => JsonSerializer.Deserialize<Dictionary<string, string>>(s, options)!,
            ValueComparer.CreateDefault(typeof(Dictionary<string, string>), true)
        );
  }
}
