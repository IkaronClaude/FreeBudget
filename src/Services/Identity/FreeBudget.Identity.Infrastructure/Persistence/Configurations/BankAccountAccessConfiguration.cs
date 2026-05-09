using FreeBudget.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeBudget.Identity.Infrastructure.Persistence.Configurations;

internal sealed class BankAccountAccessConfiguration : IEntityTypeConfiguration<BankAccountAccess>
{
    public void Configure(EntityTypeBuilder<BankAccountAccess> builder)
    {
        builder.ToTable("bank_account_access");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.BankAccountId)
            .HasColumnName("bank_account_id")
            .IsRequired();

        builder.Property(a => a.GroupId)
            .HasColumnName("group_id")
            .IsRequired();

        builder.Property(a => a.GrantedAt)
            .HasColumnName("granted_at")
            .IsRequired();

        builder.HasIndex(a => new { a.BankAccountId, a.GroupId }).IsUnique();
    }
}
