using Core.Domain.NotificationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;
public class NotifConfig : IEntityTypeConfiguration<Notif>
{
    public void Configure(EntityTypeBuilder<Notif> builder)
    {
    }
}
