using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Transactions.Domain.Entities;

public sealed class ImportLayoutDefinition : Entity<Guid>, IAuditableEntity
{
    private ImportLayoutDefinition() { }

    public Guid BankAccountId { get; private init; }
    public Guid CreatedByUserId { get; private init; }
    public string Name { get; private set; } = null!;
    public string DateColumn { get; private set; } = null!;
    public string DescriptionColumn { get; private set; } = null!;
    public string AmountColumn { get; private set; } = null!;
    public string? CurrencyColumn { get; private set; }
    public string? DirectionColumn { get; private set; }
    public Dictionary<string, string> DirectionMappings { get; private set; } = new();
    public string? ExternalIdColumn { get; private set; }
    public string? RunningBalanceColumn { get; private set; }
    public string? CategoryColumn { get; private set; }
    public string? TargetAmountColumn { get; private set; }
    public string? TargetCurrencyColumn { get; private set; }
    public string DateFormat { get; private set; } = "dd/MM/yyyy";
    public bool HasHeaderRow { get; private set; } = true;
    public string Delimiter { get; private set; } = ",";
    public string DefaultCurrencyCode { get; private set; } = "GBP";
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public static ImportLayoutDefinition Create(
        Guid bankAccountId,
        Guid createdByUserId,
        string name,
        string dateColumn,
        string descriptionColumn,
        string amountColumn,
        string? currencyColumn = null,
        string? directionColumn = null,
        Dictionary<string, string>? directionMappings = null,
        string? externalIdColumn = null,
        string? runningBalanceColumn = null,
        string? categoryColumn = null,
        string? targetAmountColumn = null,
        string? targetCurrencyColumn = null,
        string dateFormat = "dd/MM/yyyy",
        bool hasHeaderRow = true,
        string delimiter = ",",
        string defaultCurrencyCode = "GBP")
    {
        if (bankAccountId == Guid.Empty)
            throw new ArgumentException("Bank account ID cannot be empty.", nameof(bankAccountId));
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("Creator user ID cannot be empty.", nameof(createdByUserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(dateColumn);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptionColumn);
        ArgumentException.ThrowIfNullOrWhiteSpace(amountColumn);
        ArgumentException.ThrowIfNullOrWhiteSpace(dateFormat);
        ArgumentException.ThrowIfNullOrWhiteSpace(delimiter);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultCurrencyCode);

        return new ImportLayoutDefinition
        {
            Id = Guid.NewGuid(),
            BankAccountId = bankAccountId,
            CreatedByUserId = createdByUserId,
            Name = name.Trim(),
            DateColumn = dateColumn.Trim(),
            DescriptionColumn = descriptionColumn.Trim(),
            AmountColumn = amountColumn.Trim(),
            CurrencyColumn = Trim(currencyColumn),
            DirectionColumn = Trim(directionColumn),
            DirectionMappings = directionMappings ?? new Dictionary<string, string>(),
            ExternalIdColumn = Trim(externalIdColumn),
            RunningBalanceColumn = Trim(runningBalanceColumn),
            CategoryColumn = Trim(categoryColumn),
            TargetAmountColumn = Trim(targetAmountColumn),
            TargetCurrencyColumn = Trim(targetCurrencyColumn),
            DateFormat = dateFormat,
            HasHeaderRow = hasHeaderRow,
            Delimiter = delimiter,
            DefaultCurrencyCode = defaultCurrencyCode.Trim().ToUpperInvariant(),
        };
    }

    public void Update(
        string name,
        string dateColumn,
        string descriptionColumn,
        string amountColumn,
        string? currencyColumn,
        string? directionColumn,
        Dictionary<string, string>? directionMappings,
        string? externalIdColumn,
        string? runningBalanceColumn,
        string? categoryColumn,
        string? targetAmountColumn,
        string? targetCurrencyColumn,
        string dateFormat,
        bool hasHeaderRow,
        string delimiter,
        string defaultCurrencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(dateColumn);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptionColumn);
        ArgumentException.ThrowIfNullOrWhiteSpace(amountColumn);
        ArgumentException.ThrowIfNullOrWhiteSpace(dateFormat);
        ArgumentException.ThrowIfNullOrWhiteSpace(delimiter);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultCurrencyCode);

        Name = name.Trim();
        DateColumn = dateColumn.Trim();
        DescriptionColumn = descriptionColumn.Trim();
        AmountColumn = amountColumn.Trim();
        CurrencyColumn = Trim(currencyColumn);
        DirectionColumn = Trim(directionColumn);
        DirectionMappings = directionMappings ?? new Dictionary<string, string>();
        ExternalIdColumn = Trim(externalIdColumn);
        RunningBalanceColumn = Trim(runningBalanceColumn);
        CategoryColumn = Trim(categoryColumn);
        TargetAmountColumn = Trim(targetAmountColumn);
        TargetCurrencyColumn = Trim(targetCurrencyColumn);
        DateFormat = dateFormat;
        HasHeaderRow = hasHeaderRow;
        Delimiter = delimiter;
        DefaultCurrencyCode = defaultCurrencyCode.Trim().ToUpperInvariant();
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
