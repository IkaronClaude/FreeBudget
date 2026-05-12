using FreeBudget.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeBudget.Identity.Infrastructure.Persistence.Configurations;

internal sealed class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.ToTable("group_members");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.GroupId)
            .HasColumnName("group_id")
            .IsRequired();

        builder.Property(m => m.Label)
            .HasColumnName("label")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.OwningUserId)
            .HasColumnName("owning_user_id");

        builder.Property(m => m.Role)
            .HasConversion<string>()
            .HasColumnName("role")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(m => m.GroupId);
        builder.HasIndex(m => m.OwningUserId);
    }
}
