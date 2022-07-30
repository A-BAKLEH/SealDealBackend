
using Clean.Architecture.Core.AgencyAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;
public class AgencyConfiguration : IEntityTypeConfiguration<Agency>
{
  public void Configure(EntityTypeBuilder<Agency> builder)
  {
    builder.Property(b => b.AgencyStatus).HasConversion<string>();

  }

}
