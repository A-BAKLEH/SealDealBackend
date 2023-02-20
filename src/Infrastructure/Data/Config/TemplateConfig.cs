using Core.Domain.BrokerAggregate.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;
public class TemplateConfig : IEntityTypeConfiguration<Template>
{
  public void Configure(EntityTypeBuilder<Template> builder)
  {
    builder.Property(p => p.TimesUsed).IsConcurrencyToken();
  }
}
