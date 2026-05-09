using FreeBudget.Transactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeBudget.Transactions.Infrastructure.Persistence.Configurations;

internal sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("import_batches");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id");

        builder.Property(b => b.BankAccountId)
            .HasColumnName("bank_account_id")
            .IsRequired();

        builder.HasIndex(b => b.BankAccountId);

        builder.Property(b => b.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(b => b.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.TransactionCount)
            .HasColumnName("transaction_count")
            .IsRequired();

        builder.Property(b => b.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(2000);
    }
}
