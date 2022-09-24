using Clean.Architecture.Core.Domain.ActionPlanAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;
public class ActionTrackerConfig : IEntityTypeConfiguration<ActionTracker>
{
  public void Configure(EntityTypeBuilder<ActionTracker> builder)
  {
    builder.HasKey(x => new { x.ActionPlanAssociationId, x.TrackedActionId });
    builder.HasOne(at => at.TrackedAction)
            .WithMany(action => action.ActionTrackers)
            .HasForeignKey(at => at.TrackedActionId)
            .OnDelete(DeleteBehavior.Cascade);
    builder.HasOne(at => at.ActionPlanAssociation)
        .WithMany(actionPlanAssociation => actionPlanAssociation.ActionTrackers)
        .HasForeignKey(at => at.ActionPlanAssociationId)
        .OnDelete(DeleteBehavior.ClientCascade);
  }
}
