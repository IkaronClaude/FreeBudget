using System.Text.RegularExpressions;
using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Identity.Domain.ValueObjects;

public sealed partial class Email : ValueObject
{
    private Email(string value) => Value = value;

    public string Value { get; }

    public static Email Create(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var trimmed = email.Trim().ToLowerInvariant();

        if (!EmailPattern().IsMatch(trimmed))
            throw new ArgumentException("Invalid email format.", nameof(email));

        return new Email(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(Email email) => email.Value;

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
