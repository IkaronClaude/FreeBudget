using FreeBudget.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeBudget.Identity.Infrastructure.Persistence.Configurations;

internal sealed class GroupMembershipConfiguration : IEntityTypeConfiguration<GroupMembership>
{
    public void Configure(EntityTypeBuilder<GroupMembership> builder)
    {
        builder.ToTable("group_memberships");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.GroupId)
            .HasColumnName("group_id")
            .IsRequired();

        builder.Property(m => m.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(m => m.Role)
            .HasConversion<string>()
            .HasColumnName("role")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(m => new { m.GroupId, m.UserId }).IsUnique();
    }
}
