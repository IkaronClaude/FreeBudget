using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Identity.Infrastructure.Persistence.Repositories;

internal sealed class GroupRepository(IdentityDbContext context) : IGroupRepository
{
    public async Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Group>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.Groups
            .Include(g => g.Members)
            .Where(g => g.Members.Any(m => m.OwningUserId == userId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Group>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Groups
            .Include(g => g.Members)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Group group, CancellationToken cancellationToken = default)
    {
        await context.Groups.AddAsync(group, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Group group, CancellationToken cancellationToken = default)
    {
        // The Group root + members already loaded by GetByIdAsync are tracked.
        // For members added via group.AddMember in the handler, EF's
        // DetectChanges discovers them but marks them Modified (since their
        // generated Guid Id is non-default) — that causes EF to issue an
        // UPDATE for a row that does not exist yet ("expected 1 row affected,
        // got 0"). Fix it by comparing against the actual stored ids and
        // flipping unknown ones to Added.
        var storedMemberIds = await context.GroupMembers
            .Where(m => m.GroupId == group.Id)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);
        var storedSet = storedMemberIds.ToHashSet();

        foreach (var member in group.Members)
        {
            if (!storedSet.Contains(member.Id))
                context.Entry(member).State = EntityState.Added;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Group group, CancellationToken cancellationToken = default)
    {
        context.Groups.Remove(group);
        await context.SaveChangesAsync(cancellationToken);
    }
}
