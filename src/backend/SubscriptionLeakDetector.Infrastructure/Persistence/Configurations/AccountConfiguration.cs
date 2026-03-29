using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionLeakDetector.Domain.Entities;

namespace SubscriptionLeakDetector.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> b)
    {
        b.ToTable("accounts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.UiCulture).HasMaxLength(32).IsRequired();
        b.Property(x => x.DefaultCurrency).HasMaxLength(3).IsRequired();
        b.HasIndex(x => x.Name);
    }
}
