using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionLeakDetector.Domain.Entities;

namespace SubscriptionLeakDetector.Infrastructure.Persistence.Configurations;

public class TransactionImportConfiguration : IEntityTypeConfiguration<TransactionImport>
{
    public void Configure(EntityTypeBuilder<TransactionImport> b)
    {
        b.ToTable("transaction_imports");
        b.HasKey(x => x.Id);
        b.Property(x => x.FileName).HasMaxLength(512).IsRequired();
        b.Property(x => x.ErrorMessage).HasMaxLength(2000);
        b.HasOne(x => x.Account).WithMany(a => a.TransactionImports).HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
