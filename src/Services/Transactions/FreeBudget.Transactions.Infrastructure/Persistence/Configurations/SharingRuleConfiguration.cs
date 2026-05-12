using System.Text.Json;
using FreeBudget.Transactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeBudget.Transactions.Infrastructure.Persistence.Configurations;

internal sealed class SharingRuleConfiguration : IEntityTypeConfiguration<SharingRule>
{
    public void Configure(EntityTypeBuilder<SharingRule> builder)
    {
        builder.ToTable("sharing_rules");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.HasIndex(r => r.CreatedByUserId);

        builder.Property(r => r.Pattern).HasColumnName("pattern").HasMaxLength(500).IsRequired();
        builder.Property(r => r.RuleMatchType).HasConversion<string>().HasColumnName("match_type").HasMaxLength(20).IsRequired();
        builder.Property(r => r.Priority).HasColumnName("priority").IsRequired();
        builder.Property(r => r.GroupId).HasColumnName("group_id").IsRequired();
        builder.Property(r => r.PaidByMemberId).HasColumnName("paid_by_member_id").IsRequired();

        var listComparer = new ValueComparer<IReadOnlyList<Guid>>(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            l => l == null ? 0 : l.Aggregate(0, (h, id) => HashCode.Combine(h, id.GetHashCode())),
            l => l.ToList());

        builder.Property(r => r.ParticipantMemberIds)
            .HasColumnName("participant_member_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .Metadata.SetValueComparer(listComparer);

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.ModifiedAt).HasColumnName("modified_at");
    }
}
