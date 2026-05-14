using FreeBudget.Identity.Domain.Events;
using FreeBudget.Identity.Domain.ValueObjects;
using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Identity.Domain.Entities;

public sealed class BankAccount : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<BankAccountAccess> _accessGrants = [];

    private BankAccount() { }

    public Guid OwnerUserId { get; private init; }
    public BankType BankType { get; private init; } = null!;
    public string? Nickname { get; private set; }
    public string? ExternalAccountId { get; private set; }
    public bool HasApiCredentials { get; private set; }
    public Guid? ParentBankAccountId { get; private init; }
    public string? CurrencyCode { get; private set; }
    public IReadOnlyList<BankAccountAccess> AccessGrants => _accessGrants.AsReadOnly();
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public bool IsChild => ParentBankAccountId is not null;

    public static BankAccount Create(Guid ownerUserId, BankType bankType, string nickname, string? currencyCode = null)
    {
        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("Owner user ID cannot be empty.", nameof(ownerUserId));

        ArgumentNullException.ThrowIfNull(bankType);
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);

        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            BankType = bankType,
            Nickname = nickname.Trim(),
            CurrencyCode = NormaliseCurrency(currencyCode),
        };

        account.RaiseDomainEvent(new BankAccountLinkedEvent(account.Id, ownerUserId, bankType.Name));

        return account;
    }

    public static BankAccount CreateParent(Guid ownerUserId, BankType bankType, string nickname)
        => Create(ownerUserId, bankType, nickname);

    public static BankAccount CreateChild(BankAccount parent, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(parent);
        if (parent.IsChild)
            throw new InvalidOperationException("Cannot nest bank accounts: parent is itself a child.");
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);

        var child = new BankAccount
        {
            Id = Guid.NewGuid(),
            OwnerUserId = parent.OwnerUserId,
            BankType = parent.BankType,
            Nickname = null,
            ParentBankAccountId = parent.Id,
            CurrencyCode = NormaliseCurrency(currencyCode),
        };

        child.RaiseDomainEvent(new BankAccountLinkedEvent(child.Id, parent.OwnerUserId, parent.BankType.Name));

        return child;
    }

    public void SetExternalAccountId(string externalAccountId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalAccountId);
        ExternalAccountId = externalAccountId;
    }

    public void MarkApiCredentialsConfigured() => HasApiCredentials = true;

    public void MarkApiCredentialsRemoved() => HasApiCredentials = false;

    public void Rename(string nickname)
    {
        if (IsChild)
            throw new InvalidOperationException("Child accounts inherit their nickname from the parent; rename the parent instead.");

        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);
        Nickname = nickname.Trim();
    }

    public BankAccountAccess GrantAccessToGroup(Guid groupId)
    {
        if (groupId == Guid.Empty)
            throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));

        if (_accessGrants.Any(a => a.GroupId == groupId))
            throw new InvalidOperationException($"Group '{groupId}' already has access to this bank account.");

        var access = new BankAccountAccess(Id, groupId);
        _accessGrants.Add(access);

        RaiseDomainEvent(new BankAccountAccessGrantedEvent(Id, groupId));

        return access;
    }

    public void RevokeAccessFromGroup(Guid groupId)
    {
        var access = _accessGrants.FirstOrDefault(a => a.GroupId == groupId)
            ?? throw new InvalidOperationException($"Group '{groupId}' does not have access to this bank account.");

        _accessGrants.Remove(access);
    }

    private static string? NormaliseCurrency(string? code)
        => string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
}
