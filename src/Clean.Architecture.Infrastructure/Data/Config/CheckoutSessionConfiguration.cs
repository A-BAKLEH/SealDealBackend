
using Clean.Architecture.Core.PaymentAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clean.Architecture.Infrastructure.Data.Config;
public class CheckoutSessionConfiguration : IEntityTypeConfiguration<CheckoutSession>
{
  public void Configure(EntityTypeBuilder<CheckoutSession> builder)
  {
    builder.Property(b => b.CheckoutSessionStatus).HasConversion<string>();

  }
}
