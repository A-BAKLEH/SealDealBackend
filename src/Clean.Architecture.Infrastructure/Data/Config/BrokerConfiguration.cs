﻿using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.Core.ProjectAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;
public class BrokerConfiguration : IEntityTypeConfiguration<Broker>
{
  public void Configure(EntityTypeBuilder<Broker> builder)
  {
    builder.Property(b => b.Id).ValueGeneratedNever();

  }

}
