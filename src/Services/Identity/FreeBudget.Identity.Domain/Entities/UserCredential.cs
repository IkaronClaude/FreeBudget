using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Identity.Domain.Entities;

public sealed class UserCredential : AggregateRoot<Guid>, IAuditableEntity
{
    private UserCredential() { }

    public Guid UserId { get; private set; }
    public string PasswordHash { get; private set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public static UserCredential Create(Guid userId, string passwordHash)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new UserCredential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PasswordHash = passwordHash,
        };
    }

    public void UpdateHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }
}
