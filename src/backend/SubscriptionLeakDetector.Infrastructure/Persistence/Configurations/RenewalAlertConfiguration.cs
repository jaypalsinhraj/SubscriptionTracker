using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionLeakDetector.Domain.Entities;

namespace SubscriptionLeakDetector.Infrastructure.Persistence.Configurations;

public class RenewalAlertConfiguration : IEntityTypeConfiguration<RenewalAlert>
{
    public void Configure(EntityTypeBuilder<RenewalAlert> b)
    {
        b.ToTable("renewal_alerts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.HasOne(x => x.Account).WithMany(a => a.RenewalAlerts).HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Subscription).WithMany(s => s.Alerts).HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
