using Core.Domain.ActionPlanAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config
{
    public class ActionPlanConfig : IEntityTypeConfiguration<ActionPlan>
    {
        public void Configure(EntityTypeBuilder<ActionPlan> builder)
        {
            builder.Property(x => x.Name).HasMaxLength(30);
            builder.Property(x => x.FirstActionDelay).HasMaxLength(10);
        }
    }
}
