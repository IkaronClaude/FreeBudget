using FreeBudget.Ledger.Domain.Entities;
using FreeBudget.Ledger.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeBudget.Ledger.Infrastructure.Persistence.Configurations;

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.GroupId)
            .HasColumnName("group_id")
            .IsRequired();

        builder.HasIndex(e => e.GroupId);

        builder.Property(e => e.PaidByUserId)
            .HasColumnName("paid_by_user_id")
            .IsRequired();

        builder.HasIndex(e => e.PaidByUserId);

        builder.Property(e => e.OwedByUserId)
            .HasColumnName("owed_by_user_id")
            .IsRequired();

        builder.OwnsOne(e => e.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 4)
                .IsRequired();

            money.Property(m => m.CurrencyCode)
                .HasColumnName("amount_currency_code")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Navigation(e => e.Amount).IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.EntryType)
            .HasConversion<string>()
            .HasColumnName("entry_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.TransactionId)
            .HasColumnName("transaction_id");

        builder.HasIndex(e => e.TransactionId)
            .HasFilter("transaction_id IS NOT NULL");

        builder.Property(e => e.EntryDate)
            .HasColumnName("entry_date")
            .IsRequired();

        builder.Property(e => e.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.ModifiedAt).HasColumnName("modified_at");
    }
}
