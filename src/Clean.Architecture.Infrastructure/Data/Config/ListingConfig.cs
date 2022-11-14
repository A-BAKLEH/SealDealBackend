
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;
public class ListingConfig : IEntityTypeConfiguration<Listing>
{
  public void Configure(EntityTypeBuilder<Listing> builder)
  {
    builder.OwnsOne(listing => listing.Address);
  }
}
