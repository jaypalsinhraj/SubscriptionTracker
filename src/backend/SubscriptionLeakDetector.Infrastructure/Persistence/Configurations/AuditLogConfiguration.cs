using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionLeakDetector.Domain.Entities;

namespace SubscriptionLeakDetector.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasMaxLength(200).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(200).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(64);
        b.Property(x => x.Details).HasMaxLength(4000).IsRequired();
        b.HasIndex(x => new { x.AccountId, x.CreatedAt });
        b.HasOne(x => x.Account).WithMany(a => a.AuditLogs).HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.User).WithMany(u => u.AuditLogs).HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
