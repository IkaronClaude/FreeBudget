using System.Text.RegularExpressions;
using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Transactions.Domain.ValueObjects;

public sealed partial class Money : ValueObject
{
    private Money() { }

    public Money(decimal amount, string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);

        if (!CurrencyPattern().IsMatch(currencyCode))
            throw new ArgumentException("Currency code must be 3 uppercase letters.", nameof(currencyCode));

        Amount = amount;
        CurrencyCode = currencyCode.ToUpperInvariant();
    }

    public decimal Amount { get; private init; }
    public string CurrencyCode { get; private init; } = null!;

    public Money Negate() => new(-Amount, CurrencyCode);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.CurrencyCode);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.CurrencyCode);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return CurrencyCode;
    }

    public override string ToString() => $"{Amount} {CurrencyCode}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.CurrencyCode != right.CurrencyCode)
            throw new InvalidOperationException(
                $"Cannot combine money with different currencies: {left.CurrencyCode} and {right.CurrencyCode}.");
    }

    [GeneratedRegex(@"^[A-Za-z]{3}$")]
    private static partial Regex CurrencyPattern();
}
