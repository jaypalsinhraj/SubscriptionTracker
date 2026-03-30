using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionLeakDetector.Domain.Entities;

namespace SubscriptionLeakDetector.Infrastructure.Persistence.Configurations;

public class RecurringCandidateConfiguration : IEntityTypeConfiguration<RecurringCandidate>
{
    public void Configure(EntityTypeBuilder<RecurringCandidate> b)
    {
        b.ToTable("recurring_candidates");
        b.HasKey(x => x.Id);
        b.Property(x => x.GroupKey).HasMaxLength(600).IsRequired();
        b.Property(x => x.VendorName).HasMaxLength(500).IsRequired();
        b.Property(x => x.NormalizedMerchant).HasMaxLength(500).IsRequired();
        b.Property(x => x.ClassificationReason).HasMaxLength(500).IsRequired();
        b.Property(x => x.AverageAmount).HasPrecision(18, 2);
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.HasIndex(x => new { x.AccountId, x.GroupKey }).IsUnique();
        b.HasOne(x => x.Account).WithMany(a => a.RecurringCandidates).HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
