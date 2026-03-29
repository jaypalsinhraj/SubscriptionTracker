using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionLeakDetector.Domain.Entities;

namespace SubscriptionLeakDetector.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> b)
    {
        b.ToTable("transactions");
        b.HasKey(x => x.Id);
        b.Property(x => x.VendorName).HasMaxLength(500).IsRequired();
        b.Property(x => x.IsCredit).HasDefaultValue(false);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.RawCategory).HasMaxLength(200);
        b.HasIndex(x => new { x.AccountId, x.TransactionDate });
        b.HasOne(x => x.Account).WithMany(a => a.Transactions).HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.TransactionImport).WithMany(i => i.Transactions).HasForeignKey(x => x.TransactionImportId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
