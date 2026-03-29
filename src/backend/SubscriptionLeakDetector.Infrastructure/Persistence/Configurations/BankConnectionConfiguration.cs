using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionLeakDetector.Domain.Entities;

namespace SubscriptionLeakDetector.Infrastructure.Persistence.Configurations;

public class BankConnectionConfiguration : IEntityTypeConfiguration<BankConnection>
{
    public void Configure(EntityTypeBuilder<BankConnection> b)
    {
        b.ToTable("bank_connections");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.InstitutionName).HasMaxLength(200);
        b.HasOne(x => x.Account).WithMany(a => a.BankConnections).HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
