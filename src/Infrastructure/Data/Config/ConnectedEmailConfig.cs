using Core.Domain.BrokerAggregate.EmailConnection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;
public class ConnectedEmailConfig : IEntityTypeConfiguration<ConnectedEmail>
{
    public void Configure(EntityTypeBuilder<ConnectedEmail> builder)
    {
        //builder.Property(x => x.Email).HasMaxLength(100); reinstate after u put all migrations into 1
        builder.HasKey(x => x.Email);
        builder.HasIndex(e => e.GraphSubscriptionId);
    }
}
