using Core.Domain.NotificationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Infrastructure.Data.Config;

public class AppEventConfig : IEntityTypeConfiguration<AppEvent>
{
    public void Configure(EntityTypeBuilder<AppEvent> builder)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.General);

        builder
            .Property(x => x.Props)
            .HasConversion(
                v => JsonSerializer.Serialize(v, options),
                s => JsonSerializer.Deserialize<Dictionary<string, string>>(s, options)!,
                ValueComparer.CreateDefault(typeof(Dictionary<string, string>), true)
            );
    }
}
