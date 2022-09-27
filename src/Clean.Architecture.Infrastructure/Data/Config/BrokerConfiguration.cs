
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;
public class BrokerConfiguration : IEntityTypeConfiguration<Broker>
{
  public void Configure(EntityTypeBuilder<Broker> builder)
  {
    builder.Property(b => b.Id).ValueGeneratedNever();
    builder.HasIndex(b => b.FirstConnectedEmail);
  }

}
