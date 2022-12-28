using System;

using Clean.Architecture.Core.Domain.BrokerAggregate.EmailConnection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;
public class ConnectedEmailConfig : IEntityTypeConfiguration<ConnectedEmail>
{
  public void Configure(EntityTypeBuilder<ConnectedEmail> builder)
  {
    builder.HasIndex(e => e.GraphSubscriptionId);
  }
}
