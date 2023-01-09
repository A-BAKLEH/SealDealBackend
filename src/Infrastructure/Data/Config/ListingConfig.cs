
using Core.Domain.AgencyAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;
public class ListingConfig : IEntityTypeConfiguration<Listing>
{
  public void Configure(EntityTypeBuilder<Listing> builder)
  {
    builder.OwnsOne(listing => listing.Address);
  }
}
