using FreeBudget.Identity.Domain.Events;
using FreeBudget.Identity.Domain.ValueObjects;
using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Identity.Domain.Entities;

public sealed class User : AggregateRoot<Guid>, IAuditableEntity
{
    private User() { }

    public Email Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public static User Create(Email email, string displayName)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName.Trim(),
        };

        user.RaiseDomainEvent(new UserCreatedEvent(user.Id, email.Value));

        return user;
    }

    public void UpdateDisplayName(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName.Trim();
    }

    public void UpdateEmail(Email email)
    {
        ArgumentNullException.ThrowIfNull(email);
        Email = email;
    }
}
