
using Core.Domain.TestAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config;
public class TestBaseConfig : IEntityTypeConfiguration<TestBase>
{
  public void Configure(EntityTypeBuilder<TestBase> builder)
  {
    builder.OwnsOne(
        testBase => testBase.testJSON, ownedNavigationBuilder =>
        {
          ownedNavigationBuilder.ToJson();
          ownedNavigationBuilder.OwnsOne(testJSON => testJSON.one);
          ownedNavigationBuilder.OwnsOne(testJSON => testJSON.two);
        });
  }
}
