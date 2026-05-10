using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeBudget.Transactions.Infrastructure.Persistence.Configurations;

internal sealed class CategorizationRuleConfiguration : IEntityTypeConfiguration<CategorizationRule>
{
    public void Configure(EntityTypeBuilder<CategorizationRule> builder)
    {
        builder.ToTable("categorization_rules");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.HasIndex(r => r.CreatedByUserId);

        builder.Property(r => r.Pattern)
            .HasColumnName("pattern")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.RuleMatchType)
            .HasConversion<string>()
            .HasColumnName("match_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.Category)
            .HasColumnName("category")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Priority)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.ModifiedAt).HasColumnName("modified_at");
    }
}
