using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeBudget.Identity.Infrastructure.Persistence.Configurations;

internal sealed class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("bank_accounts");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id");

        builder.Property(b => b.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        builder.HasIndex(b => b.OwnerUserId);

        builder.Property(b => b.BankType)
            .HasConversion(bt => bt.Name, s => BankType.From(s))
            .HasColumnName("bank_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(b => b.Nickname)
            .HasColumnName("nickname")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.ExternalAccountId)
            .HasColumnName("external_account_id")
            .HasMaxLength(500);

        builder.Property(b => b.HasApiCredentials)
            .HasColumnName("has_api_credentials")
            .HasDefaultValue(false);

        builder.HasMany(b => b.AccessGrants)
            .WithOne()
            .HasForeignKey(a => a.BankAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.AccessGrants)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(b => b.ModifiedAt).HasColumnName("modified_at");
    }
}
