
using Core.Domain.AINurturingAggregate;
using Core.Domain.BrokerAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;
public class AINurturingConfiguration : IEntityTypeConfiguration<AINurturing>
{
    public void Configure(EntityTypeBuilder<AINurturing> builder)
    {

    }
}
