using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Identity.Infrastructure.Persistence.Repositories;

internal sealed class GroupRepository(IdentityDbContext context) : IGroupRepository
{
    public async Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Groups
            .Include(g => g.Memberships)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Group>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.Groups
            .Include(g => g.Memberships)
            .Where(g => g.Memberships.Any(m => m.UserId == userId))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Group group, CancellationToken cancellationToken = default)
    {
        await context.Groups.AddAsync(group, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Group group, CancellationToken cancellationToken = default)
    {
        context.Groups.Update(group);
        await context.SaveChangesAsync(cancellationToken);
    }
}
