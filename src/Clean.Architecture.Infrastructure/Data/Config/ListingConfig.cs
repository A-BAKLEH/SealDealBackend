using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.LeadAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;
/*public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
  public void Configure(EntityTypeBuilder<Listing> builder)
  {
    builder
        .HasMany(li => li.InterestedLeads)
        .WithMany(le => le.ListingsOfInterest)
        .UsingEntity<Dictionary<string, object>>(
            "ListingLead",
            j => j.HasOne<Lead>().WithMany().OnDelete(DeleteBehavior.ClientCascade),
            j => j.HasOne<Listing>().WithMany().OnDelete(DeleteBehavior.Cascade));
  }

}*/
