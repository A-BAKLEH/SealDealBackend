

using Clean.Architecture.Core.Domain.AgencyAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;
public class AgencyConfiguration : IEntityTypeConfiguration<Agency>
{
  public void Configure(EntityTypeBuilder<Agency> builder)
  {
    builder.Property(b => b.StripeSubscriptionStatus).HasConversion<string>();
    builder.Property(a => a.SubscriptionLastValidDate).HasColumnType("date");

  }

}
