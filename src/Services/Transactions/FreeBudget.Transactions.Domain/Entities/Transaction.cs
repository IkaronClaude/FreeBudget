using FreeBudget.SharedKernel.Domain;
using FreeBudget.Transactions.Domain.Events;
using FreeBudget.SharedKernel.ValueObjects;
using FreeBudget.Transactions.Domain.ValueObjects;

namespace FreeBudget.Transactions.Domain.Entities;

public sealed class Transaction : AggregateRoot<Guid>, IAuditableEntity
{
    private Transaction() { }

    public Guid BankAccountId { get; private init; }
    public DateTime TransactionDate { get; private init; }
    public string Description { get; private set; } = null!;
    public Money Amount { get; private init; } = null!;
    public TransactionDirection Direction { get; private init; } = null!;
    public Money? RunningBalance { get; private init; }
    public string? ExternalTransactionId { get; private init; }
    public Guid? ImportBatchId { get; private init; }
    public string? Category { get; private set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public static Transaction Create(
        Guid bankAccountId,
        DateTime transactionDate,
        string description,
        Money amount,
        TransactionDirection direction,
        Money? runningBalance = null,
        string? externalTransactionId = null,
        Guid? importBatchId = null,
        string? category = null)
    {
        if (bankAccountId == Guid.Empty)
            throw new ArgumentException("Bank account ID cannot be empty.", nameof(bankAccountId));

        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentNullException.ThrowIfNull(direction);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            BankAccountId = bankAccountId,
            TransactionDate = transactionDate,
            Description = description.Trim(),
            Amount = amount,
            Direction = direction,
            RunningBalance = runningBalance,
            ExternalTransactionId = externalTransactionId?.Trim(),
            ImportBatchId = importBatchId,
            Category = category?.Trim(),
        };

        transaction.RaiseDomainEvent(new TransactionImportedEvent(
            transaction.Id,
            bankAccountId,
            amount.Amount,
            amount.CurrencyCode));

        return transaction;
    }

    public void UpdateDescription(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        Description = description.Trim();
    }

    public void UpdateCategory(string? category)
    {
        Category = category?.Trim();
    }
}
