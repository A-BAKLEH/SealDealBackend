

using Core.Domain.AgencyAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;
public class AgencyConfiguration : IEntityTypeConfiguration<Agency>
{
    public void Configure(EntityTypeBuilder<Agency> builder)
    {
        builder.Property(a => a.AgencyName).HasMaxLength(50);
        builder.Property(a => a.PhoneNumber).HasMaxLength(30);

        builder.Property(b => b.StripeSubscriptionStatus).HasConversion<string>();
        builder.OwnsOne(agency => agency.Address);
        //builder.OwnsOne(agency => agency.Address, ownedNavigationBuilder =>
        //{
        //    ownedNavigationBuilder.ToJson();
        //});
    }

}
