using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserCredentialRepository(IdentityDbContext context) : IUserCredentialRepository
{
    public async Task<UserCredential?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.UserCredentials.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public async Task AddAsync(UserCredential credential, CancellationToken cancellationToken = default)
    {
        await context.UserCredentials.AddAsync(credential, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserCredential credential, CancellationToken cancellationToken = default)
    {
        context.UserCredentials.Update(credential);
        await context.SaveChangesAsync(cancellationToken);
    }
}
