using FreeBudget.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Common.Infrastructure.Persistence;

public abstract class BaseDbContext(DbContextOptions options) : DbContext(options)
{
    static BaseDbContext()
    {
        // Allow non-UTC DateTime values to be written to timestamptz columns.
        // We write UtcNow / parsed-as-UTC dates intentionally, but third-party
        // entry points (model binding, JSON deserialisation) can still produce
        // Kind=Unspecified. This switch keeps Npgsql v5-and-earlier behaviour.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

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
