using FreeBudget.SharedKernel.Domain;
using FreeBudget.Transactions.Domain.Enums;
using FreeBudget.Transactions.Domain.Events;

namespace FreeBudget.Transactions.Domain.Entities;

public sealed class ImportBatch : Entity<Guid>
{
    private ImportBatch() { }

    public Guid BankAccountId { get; private init; }
    public DateTime StartedAt { get; private init; }
    public DateTime? CompletedAt { get; private set; }
    public ImportStatus Status { get; private set; }
    public int TransactionCount { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static ImportBatch Start(Guid bankAccountId)
    {
        if (bankAccountId == Guid.Empty)
            throw new ArgumentException("Bank account ID cannot be empty.", nameof(bankAccountId));

        return new ImportBatch
        {
            Id = Guid.NewGuid(),
            BankAccountId = bankAccountId,
            StartedAt = DateTime.UtcNow,
            Status = ImportStatus.InProgress,
        };
    }

    public void MarkCompleted(int transactionCount)
    {
        EnsureInProgress();

        if (transactionCount < 0)
            throw new ArgumentOutOfRangeException(nameof(transactionCount), "Transaction count cannot be negative.");

        Status = ImportStatus.Completed;
        TransactionCount = transactionCount;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        EnsureInProgress();
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        Status = ImportStatus.Failed;
        ErrorMessage = errorMessage.Trim();
        CompletedAt = DateTime.UtcNow;
    }

    private void EnsureInProgress()
    {
        if (Status != ImportStatus.InProgress)
            throw new InvalidOperationException(
                $"Cannot transition from {Status}. Import batch must be InProgress.");
    }
}
