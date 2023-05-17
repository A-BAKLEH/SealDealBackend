using Core.Domain.ActionPlanAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Infrastructure.Data.Config;
public class ActionPlanActionConfig : IEntityTypeConfiguration<ActionPlanAction>
{
    public void Configure(EntityTypeBuilder<ActionPlanAction> builder)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.General);
        builder
            .Property(x => x.ActionProperties)
            .HasConversion(
                v => JsonSerializer.Serialize(v, options),
                s => JsonSerializer.Deserialize<Dictionary<string, string>>(s, options)!,
                ValueComparer.CreateDefault(typeof(Dictionary<string, string>), true)
            );
    }
}
