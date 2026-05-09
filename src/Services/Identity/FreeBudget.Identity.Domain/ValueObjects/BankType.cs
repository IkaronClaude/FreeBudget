using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Identity.Domain.ValueObjects;

public sealed class BankType : ValueObject
{
    public static readonly BankType Barclays = new("Barclays");
    public static readonly BankType Wise = new("Wise");
    public static readonly BankType NatWest = new("NatWest");

    private static readonly IReadOnlyDictionary<string, BankType> KnownTypes =
        new Dictionary<string, BankType>(StringComparer.OrdinalIgnoreCase)
        {
            [Barclays.Name] = Barclays,
            [Wise.Name] = Wise,
            [NatWest.Name] = NatWest,
        };

    private BankType(string name) => Name = name;

    public string Name { get; }

    public static BankType From(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!KnownTypes.TryGetValue(name, out var bankType))
            throw new ArgumentException($"Unknown bank type: '{name}'.", nameof(name));

        return bankType;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
    }

    public override string ToString() => Name;
}
