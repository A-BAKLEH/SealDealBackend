using System;

using Core.Domain.BrokerAggregate.EmailConnection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;
public class ConnectedEmailConfig : IEntityTypeConfiguration<ConnectedEmail>
{
  public void Configure(EntityTypeBuilder<ConnectedEmail> builder)
  {
    builder.HasKey(x => x.Email);
    builder.HasIndex(e => e.GraphSubscriptionId);
  }
}
