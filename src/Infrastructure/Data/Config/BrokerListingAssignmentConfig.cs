using Core.Domain.BrokerAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;
public class BrokerListingAssignmentConfig : IEntityTypeConfiguration<BrokerListingAssignment>
{
  public void Configure(EntityTypeBuilder<BrokerListingAssignment> builder)
  {
    builder.HasKey(x => new { x.BrokerId, x.ListingId });


    builder.HasOne(bl => bl.Broker)
            .WithMany(broker => broker.AssignedListings)
            .HasForeignKey(bl => bl.BrokerId)
            .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(bl => bl.Listing)
        .WithMany(listing => listing.BrokersAssigned)
        .HasForeignKey(bl => bl.ListingId)
        .OnDelete(DeleteBehavior.ClientCascade);
  }
}
