using System.Text.Json;
using FreeBudget.Transactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeBudget.Transactions.Infrastructure.Persistence.Configurations;

internal sealed class ImportLayoutDefinitionConfiguration : IEntityTypeConfiguration<ImportLayoutDefinition>
{
    public void Configure(EntityTypeBuilder<ImportLayoutDefinition> builder)
    {
        builder.ToTable("import_layouts");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");

        builder.Property(l => l.BankAccountId).HasColumnName("bank_account_id").IsRequired();
        builder.HasIndex(l => l.BankAccountId).IsUnique();

        builder.Property(l => l.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder.Property(l => l.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(l => l.DateColumn).HasColumnName("date_column").HasMaxLength(200).IsRequired();
        builder.Property(l => l.DescriptionColumn).HasColumnName("description_column").HasMaxLength(200).IsRequired();
        builder.Property(l => l.AmountColumn).HasColumnName("amount_column").HasMaxLength(200).IsRequired();
        builder.Property(l => l.CurrencyColumn).HasColumnName("currency_column").HasMaxLength(200);
        builder.Property(l => l.DirectionColumn).HasColumnName("direction_column").HasMaxLength(200);
        builder.Property(l => l.ExternalIdColumn).HasColumnName("external_id_column").HasMaxLength(200);
        builder.Property(l => l.RunningBalanceColumn).HasColumnName("running_balance_column").HasMaxLength(200);
        builder.Property(l => l.CategoryColumn).HasColumnName("category_column").HasMaxLength(200);
        builder.Property(l => l.DateFormat).HasColumnName("date_format").HasMaxLength(50).IsRequired();
        builder.Property(l => l.HasHeaderRow).HasColumnName("has_header_row").IsRequired();
        builder.Property(l => l.Delimiter).HasColumnName("delimiter").HasMaxLength(4).IsRequired();
        builder.Property(l => l.DefaultCurrencyCode).HasColumnName("default_currency_code").HasMaxLength(3).IsRequired();

        var dictComparer = new ValueComparer<Dictionary<string, string>>(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            d => d == null ? 0 : d.Aggregate(0, (h, kv) => HashCode.Combine(h, kv.Key.GetHashCode(), kv.Value.GetHashCode())),
            d => new Dictionary<string, string>(d));

        builder.Property(l => l.DirectionMappings)
            .HasColumnName("direction_mappings")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
            .Metadata.SetValueComparer(dictComparer);

        builder.Property(l => l.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(l => l.ModifiedAt).HasColumnName("modified_at");
    }
}
