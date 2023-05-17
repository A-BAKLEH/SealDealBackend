using Core.Domain.LeadAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;

public class LeadEmailConfig : IEntityTypeConfiguration<LeadEmail>
{
    public void Configure(EntityTypeBuilder<LeadEmail> builder)
    {
        builder.Property(b => b.EmailAddress).HasMaxLength(60);
        builder.HasKey(b => new {b.EmailAddress,b.LeadId});
    }
}
