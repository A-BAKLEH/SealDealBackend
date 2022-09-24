using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Clean.Architecture.Core.Domain.LeadAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;
public class LeadListingConfig : IEntityTypeConfiguration<LeadListing>
{
  public void Configure(EntityTypeBuilder<LeadListing> builder)
  {
    builder.HasKey(x => new {x.LeadId, x.ListingId});


    builder.HasOne(ll => ll.Lead)
            .WithMany(lead => lead.ListingsOfInterest)
            .HasForeignKey(ll => ll.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(ll => ll.Listing)
        .WithMany(listing => listing.InterestedLeads)
        .HasForeignKey(ll => ll.ListingId)
        .OnDelete(DeleteBehavior.ClientCascade);
  }
}
