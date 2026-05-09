using FreeBudget.SharedKernel.Domain;
using FreeBudget.Transactions.Domain.Enums;

namespace FreeBudget.Transactions.Domain.Entities;

public sealed class CategorizationRule : Entity<Guid>, IAuditableEntity
{
    private CategorizationRule() { }

    public Guid CreatedByUserId { get; private init; }
    public string Pattern { get; private set; } = null!;
    public RuleMatchType RuleMatchType { get; private set; }
    public string Category { get; private set; } = null!;
    public int Priority { get; private set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public static CategorizationRule Create(
        Guid createdByUserId,
        string pattern,
        RuleMatchType matchType,
        string category,
        int priority = 0)
    {
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(createdByUserId));

        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        return new CategorizationRule
        {
            Id = Guid.NewGuid(),
            CreatedByUserId = createdByUserId,
            Pattern = pattern.Trim(),
            RuleMatchType = matchType,
            Category = category.Trim(),
            Priority = priority,
        };
    }

    public void Update(string pattern, RuleMatchType matchType, string category, int priority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        Pattern = pattern.Trim();
        RuleMatchType = matchType;
        Category = category.Trim();
        Priority = priority;
    }

    public bool Matches(string description)
    {
        return RuleMatchType switch
        {
            RuleMatchType.Contains => description.Contains(Pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchType.Exact => description.Equals(Pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchType.StartsWith => description.StartsWith(Pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchType.EndsWith => description.EndsWith(Pattern, StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }
}
