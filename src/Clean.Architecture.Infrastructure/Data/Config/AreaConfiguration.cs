using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.LeadAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Clean.Architecture.Infrastructure.Data.Config;
public  class AreaConfiguration: IEntityTypeConfiguration<Area>
{
  public void Configure(EntityTypeBuilder<Area> builder)
  {
    builder
         .HasMany(a => a.InterestedLeads)
         .WithMany(l => l.AreasOfInterest)
         .UsingEntity<Dictionary<string, object>>(
             "AreaLead",
             j => j.HasOne<Lead>().WithMany().OnDelete(DeleteBehavior.ClientCascade),
             j => j.HasOne<Area>().WithMany().OnDelete(DeleteBehavior.Cascade));

  }
}
