using Core.Domain.ActionPlanAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Infrastructure.Data.Config
{
    public class ActionPlanAssociationConfig : IEntityTypeConfiguration<ActionPlanAssociation>
    {
        public void Configure(EntityTypeBuilder<ActionPlanAssociation> builder)
        {
            builder.Property(x => x.CustomDelay).HasMaxLength(10);
        }
    }
}
