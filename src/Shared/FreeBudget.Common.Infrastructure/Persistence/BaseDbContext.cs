using FreeBudget.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Common.Infrastructure.Persistence;

public abstract class BaseDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AggregateRoot<Guid>).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Ignore("DomainEvents");
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditableFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditableFields()
    {
        var entries = ChangeTracker.Entries<IAuditableEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.UtcNow;

            if (entry.State == EntityState.Modified)
                entry.Entity.ModifiedAt = DateTime.UtcNow;
        }
    }
}
