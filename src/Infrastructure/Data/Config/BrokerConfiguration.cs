
using Core.Domain.BrokerAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;
public class BrokerConfiguration : IEntityTypeConfiguration<Broker>
{
    public void Configure(EntityTypeBuilder<Broker> builder)
    {
        builder.Property(b => b.FirstName).HasMaxLength(30);
        builder.Property(b => b.LastName).HasMaxLength(30);
        builder.Property(b => b.LoginEmail).HasMaxLength(100);
        builder.Property(b => b.PhoneNumber).HasMaxLength(30);
        builder.Property(b => b.TimeZoneId).HasMaxLength(50);
        builder.Property(b => b.TempTimeZone).HasMaxLength(50);

        builder.HasMany(x => x.AINurturings).WithOne(b => b.broker).HasForeignKey(b => b.BrokerId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(b => b.Id).ValueGeneratedNever();
    }

}
