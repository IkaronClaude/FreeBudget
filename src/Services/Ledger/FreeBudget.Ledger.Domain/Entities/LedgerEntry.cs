using FreeBudget.Ledger.Domain.Enums;
using FreeBudget.Ledger.Domain.Events;
using FreeBudget.SharedKernel.Domain;
using FreeBudget.SharedKernel.ValueObjects;

namespace FreeBudget.Ledger.Domain.Entities;

public sealed class LedgerEntry : AggregateRoot<Guid>, IAuditableEntity
{
    private LedgerEntry() { }

    public Guid GroupId { get; private init; }
    public Guid PaidByUserId { get; private init; }
    public Guid OwedByUserId { get; private init; }
    public Money Amount { get; private init; } = null!;
    public string Description { get; private set; } = null!;
    public LedgerEntryType EntryType { get; private init; }
    public Guid? TransactionId { get; private set; }
    public DateTime EntryDate { get; private init; }
    public Guid CreatedByUserId { get; private init; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public static LedgerEntry CreateExpense(
        Guid groupId,
        Guid paidByUserId,
        Guid owedByUserId,
        Money amount,
        string description,
        DateTime entryDate,
        Guid createdByUserId,
        Guid? transactionId = null)
    {
        return Create(groupId, paidByUserId, owedByUserId, amount, description,
            LedgerEntryType.Expense, entryDate, createdByUserId, transactionId);
    }

    public static LedgerEntry CreateSettlement(
        Guid groupId,
        Guid paidByUserId,
        Guid owedByUserId,
        Money amount,
        string description,
        DateTime entryDate,
        Guid createdByUserId,
        Guid? transactionId = null)
    {
        return Create(groupId, paidByUserId, owedByUserId, amount, description,
            LedgerEntryType.Settlement, entryDate, createdByUserId, transactionId);
    }

    public void LinkTransaction(Guid transactionId)
    {
        if (transactionId == Guid.Empty)
            throw new ArgumentException("Transaction ID cannot be empty.", nameof(transactionId));

        TransactionId = transactionId;
    }

    private static LedgerEntry Create(
        Guid groupId,
        Guid paidByUserId,
        Guid owedByUserId,
        Money amount,
        string description,
        LedgerEntryType entryType,
        DateTime entryDate,
        Guid createdByUserId,
        Guid? transactionId)
    {
        if (groupId == Guid.Empty)
            throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));
        if (paidByUserId == Guid.Empty)
            throw new ArgumentException("PaidBy user ID cannot be empty.", nameof(paidByUserId));
        if (owedByUserId == Guid.Empty)
            throw new ArgumentException("OwedBy user ID cannot be empty.", nameof(owedByUserId));
        if (paidByUserId == owedByUserId)
            throw new ArgumentException("Cannot create a ledger entry where a user owes themselves.");
        ArgumentNullException.ThrowIfNull(amount);
        if (amount.Amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedBy user ID cannot be empty.", nameof(createdByUserId));

        var entry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            PaidByUserId = paidByUserId,
            OwedByUserId = owedByUserId,
            Amount = amount,
            Description = description.Trim(),
            EntryType = entryType,
            TransactionId = transactionId,
            EntryDate = entryDate,
            CreatedByUserId = createdByUserId,
        };

        entry.RaiseDomainEvent(new LedgerEntryCreatedEvent(
            entry.Id,
            groupId,
            paidByUserId,
            owedByUserId,
            amount.Amount,
            amount.CurrencyCode,
            entryType));

        return entry;
    }
}
