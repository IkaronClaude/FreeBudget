using FreeBudget.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeBudget.Identity.Infrastructure.Persistence.Configurations;

internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id");

        builder.Property(g => g.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(g => g.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.HasIndex(g => g.CreatedByUserId);

        builder.HasMany(g => g.Memberships)
            .WithOne()
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(g => g.Memberships)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(g => g.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(g => g.ModifiedAt).HasColumnName("modified_at");
    }
}
