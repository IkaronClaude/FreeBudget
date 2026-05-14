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
    {
        // Accounts directly granted, plus children whose parent is granted.
        var directlyGrantedIds = await context.BankAccounts
            .Where(b => b.AccessGrants.Any(a => a.GroupId == groupId))
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        return await context.BankAccounts
            .Include(b => b.AccessGrants)
            .Where(b => directlyGrantedIds.Contains(b.Id)
                     || (b.ParentBankAccountId != null && directlyGrantedIds.Contains(b.ParentBankAccountId.Value)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BankAccount>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
        => await context.BankAccounts
            .Where(b => b.ParentBankAccountId == parentId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
    {
        await context.BankAccounts.AddAsync(bankAccount, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<BankAccount> bankAccounts, CancellationToken cancellationToken = default)
    {
        await context.BankAccounts.AddRangeAsync(bankAccounts, cancellationToken);
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
