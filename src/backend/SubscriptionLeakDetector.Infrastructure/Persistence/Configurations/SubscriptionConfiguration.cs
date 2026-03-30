using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionLeakDetector.Domain.Entities;
using UserEntity = SubscriptionLeakDetector.Domain.Entities.User;

namespace SubscriptionLeakDetector.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.ToTable("subscriptions");
        b.HasKey(x => x.Id);
        b.Property(x => x.VendorName).HasMaxLength(500).IsRequired();
        b.Property(x => x.NormalizedMerchant).HasMaxLength(500).IsRequired();
        b.Property(x => x.ClassificationReason).HasMaxLength(500).IsRequired();
        b.Property(x => x.IsSubscriptionCandidate).HasDefaultValue(true);
        b.Property(x => x.AverageAmount).HasPrecision(18, 2);
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.Property(x => x.OwnerName).HasMaxLength(200);
        b.Property(x => x.OwnerEmail).HasMaxLength(320);
        b.HasIndex(x => new { x.AccountId, x.VendorName });
        b.HasOne(x => x.Account).WithMany(a => a.Subscriptions).HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne<UserEntity>().WithMany().HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
