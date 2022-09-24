using System.Text.Json;
using Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;
public class ActionBaseConfig : IEntityTypeConfiguration<ActionBase>
{
  public void Configure(EntityTypeBuilder<ActionBase> builder)
  {
    ////builder.ToTable("Actions");
    var options = new JsonSerializerOptions(JsonSerializerDefaults.General);

    builder
        .Property(x => x.ActionProperties)
        //.HasColumnName("Values")
        //.HasColumnType("BLOB")
        .HasConversion(
            v => JsonSerializer.Serialize(v, options),
            s => JsonSerializer.Deserialize<Dictionary<string, string>>(s, options)!,
            ValueComparer.CreateDefault(typeof(Dictionary<string, string>), true)
        );
  }
}
