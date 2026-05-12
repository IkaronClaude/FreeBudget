using FreeBudget.Ledger.Domain.Enums;
using FreeBudget.Ledger.Domain.Events;
using FreeBudget.SharedKernel.Domain;
using FreeBudget.SharedKernel.ValueObjects;

namespace FreeBudget.Ledger.Domain.Entities;

public sealed class LedgerEntry : AggregateRoot<Guid>, IAuditableEntity
{
    private LedgerEntry() { }

    public Guid GroupId { get; private init; }
    public Guid PaidByMemberId { get; private init; }
    public Guid OwedByMemberId { get; private init; }
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
        Guid paidByMemberId,
        Guid owedByMemberId,
        Money amount,
        string description,
        DateTime entryDate,
        Guid createdByUserId,
        Guid? transactionId = null)
    {
        return Create(groupId, paidByMemberId, owedByMemberId, amount, description,
            LedgerEntryType.Expense, entryDate, createdByUserId, transactionId);
    }

    public static LedgerEntry CreateSettlement(
        Guid groupId,
        Guid paidByMemberId,
        Guid owedByMemberId,
        Money amount,
        string description,
        DateTime entryDate,
        Guid createdByUserId,
        Guid? transactionId = null)
    {
        return Create(groupId, paidByMemberId, owedByMemberId, amount, description,
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
        Guid paidByMemberId,
        Guid owedByMemberId,
        Money amount,
        string description,
        LedgerEntryType entryType,
        DateTime entryDate,
        Guid createdByUserId,
        Guid? transactionId)
    {
        if (groupId == Guid.Empty)
            throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));
        if (paidByMemberId == Guid.Empty)
            throw new ArgumentException("PaidBy member ID cannot be empty.", nameof(paidByMemberId));
        if (owedByMemberId == Guid.Empty)
            throw new ArgumentException("OwedBy member ID cannot be empty.", nameof(owedByMemberId));
        if (paidByMemberId == owedByMemberId)
            throw new ArgumentException("Cannot create a ledger entry where a member owes themselves.");
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
            PaidByMemberId = paidByMemberId,
            OwedByMemberId = owedByMemberId,
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
            paidByMemberId,
            owedByMemberId,
            amount.Amount,
            amount.CurrencyCode,
            entryType));

        return entry;
    }
}
