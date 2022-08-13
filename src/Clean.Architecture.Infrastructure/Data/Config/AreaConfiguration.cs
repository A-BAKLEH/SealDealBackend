

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;
/*public  class AreaConfiguration: IEntityTypeConfiguration<Area>
{
  public void Configure(EntityTypeBuilder<Area> builder)
  {
    builder.HasMany(p => p.InterestedLeads)
    .WithMany(p => p.AreasOfInterest)
    .UsingEntity<Dictionary<string, object>>(
        "PostTag",
        j => j
            .HasOne<Lead>()
            .WithMany()
            .HasForeignKey("LeadId")
            .HasConstraintName("FK_PostTag_Leads_LeadId")
            .OnDelete(DeleteBehavior.Cascade),
        j => j
            .HasOne<Area>()
            .WithMany()
            .HasForeignKey("Id")
            .HasConstraintName("FK_PostTag_Posts_PostId")
            .OnDelete(DeleteBehavior.ClientCascade));

  }
}*/
