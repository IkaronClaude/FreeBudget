using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Identity.Infrastructure.Persistence.Repositories;

internal sealed class BankAccountRepository(IdentityDbContext context) : IBankAccountRepository
{
    public async Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.BankAccounts
            .Include(b => b.AccessGrants)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BankAccount>> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
        => await context.BankAccounts
            .Include(b => b.AccessGrants)
            .Where(b => b.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BankAccount>> GetByGroupAccessAsync(Guid groupId, CancellationToken cancellationToken = default)
        => await context.BankAccounts
            .Include(b => b.AccessGrants)
            .Where(b => b.AccessGrants.Any(a => a.GroupId == groupId))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
    {
        await context.BankAccounts.AddAsync(bankAccount, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
    {
        context.BankAccounts.Update(bankAccount);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
    {
        context.BankAccounts.Remove(bankAccount);
        await context.SaveChangesAsync(cancellationToken);
    }
}
