using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Transactions.Domain.ValueObjects;

public sealed class TransactionDirection : ValueObject
{
    public static readonly TransactionDirection Credit = new("Credit");
    public static readonly TransactionDirection Debit = new("Debit");

    private static readonly IReadOnlyDictionary<string, TransactionDirection> KnownDirections =
        new Dictionary<string, TransactionDirection>(StringComparer.OrdinalIgnoreCase)
        {
            [Credit.Value] = Credit,
            [Debit.Value] = Debit,
        };

    private TransactionDirection(string value) => Value = value;

    public string Value { get; }

    public static TransactionDirection From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!KnownDirections.TryGetValue(value, out var direction))
            throw new ArgumentException($"Unknown transaction direction: '{value}'.", nameof(value));

        return direction;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
