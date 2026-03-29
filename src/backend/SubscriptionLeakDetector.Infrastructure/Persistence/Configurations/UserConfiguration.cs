using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionLeakDetector.Domain.Entities;

namespace SubscriptionLeakDetector.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.ExternalId).HasMaxLength(128).IsRequired();
        b.Property(x => x.Email).HasMaxLength(320).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(200);
        b.HasIndex(x => new { x.AccountId, x.ExternalId }).IsUnique();
        b.HasOne(x => x.Account).WithMany(a => a.Users).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
    }
}
