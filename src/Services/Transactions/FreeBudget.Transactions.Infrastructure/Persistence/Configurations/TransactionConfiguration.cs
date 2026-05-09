using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeBudget.Transactions.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.BankAccountId)
            .HasColumnName("bank_account_id")
            .IsRequired();

        builder.HasIndex(t => t.BankAccountId);

        builder.Property(t => t.TransactionDate)
            .HasColumnName("transaction_date")
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(1000)
            .IsRequired();

        builder.OwnsOne(t => t.Amount, money =>
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

        builder.Navigation(t => t.Amount).IsRequired();

        builder.Property(t => t.Direction)
            .HasConversion(d => d.Value, s => TransactionDirection.From(s))
            .HasColumnName("direction")
            .HasMaxLength(10)
            .IsRequired();

        builder.OwnsOne(t => t.RunningBalance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("running_balance_amount")
                .HasPrecision(18, 4);

            money.Property(m => m.CurrencyCode)
                .HasColumnName("running_balance_currency_code")
                .HasMaxLength(3);
        });

        builder.Navigation(t => t.RunningBalance).IsRequired(false);

        builder.Property(t => t.Category)
            .HasColumnName("category")
            .HasMaxLength(200);

        builder.HasIndex(t => t.Category);

        builder.Property(t => t.ExternalTransactionId)
            .HasColumnName("external_transaction_id")
            .HasMaxLength(500);

        builder.Property(t => t.ImportBatchId)
            .HasColumnName("import_batch_id");

        builder.HasIndex(t => t.ImportBatchId);

        builder.HasIndex(t => new { t.BankAccountId, t.ExternalTransactionId })
            .IsUnique()
            .HasFilter("external_transaction_id IS NOT NULL");

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.ModifiedAt).HasColumnName("modified_at");
    }
}
