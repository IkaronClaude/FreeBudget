using FreeBudget.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Common.Infrastructure.Persistence;

public abstract class BaseDbContext(DbContextOptions options) : DbContext(options)
{
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
