using FreeBudget.Identity.Domain.Entities;

namespace FreeBudget.Identity.Application.Interfaces;

public interface IUserCredentialRepository
{
    Task<UserCredential?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserCredential credential, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserCredential credential, CancellationToken cancellationToken = default);
}
