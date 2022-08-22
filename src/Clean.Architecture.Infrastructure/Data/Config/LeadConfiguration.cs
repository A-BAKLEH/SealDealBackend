

using Clean.Architecture.Core.Domain.LeadAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;
public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
  public void Configure(EntityTypeBuilder<Lead> builder)
  {
    builder.Property(b => b.LeadStatus).HasConversion<string>();

  }
}
