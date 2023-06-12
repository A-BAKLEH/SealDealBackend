
using Core.Domain.AgencyAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;
public class ListingConfig : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.Property(a => a.FormattedStreetAddress).HasMaxLength(50);
        builder.HasIndex(a => new { a.AgencyId,a.FormattedStreetAddress });
        //builder.OwnsOne(listing => listing.Address, ownedNavigationBuilder =>
        //{
        //    ownedNavigationBuilder.ToJson();
        //});
        builder.OwnsOne(listing => listing.Address);
    }
}
