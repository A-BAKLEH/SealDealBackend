using Core.Domain.NotificationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;

public class EmailEventConfig : IEntityTypeConfiguration<EmailEvent>
{
    public void Configure(EntityTypeBuilder<EmailEvent> builder)
    {
        builder.Property(b => b.BrokerEmail).HasMaxLength(80);
    }
}
