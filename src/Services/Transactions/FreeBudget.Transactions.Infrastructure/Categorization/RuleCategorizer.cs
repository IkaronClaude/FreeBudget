using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;

namespace FreeBudget.Transactions.Infrastructure.Categorization;

internal sealed class RuleCategorizer : ICategorizer
{
    public string? Categorize(string description, IReadOnlyList<CategorizationRule> rules)
    {
        if (rules.Count == 0)
            return null;

        var match = rules
            .OrderByDescending(r => r.Priority)
            .FirstOrDefault(r => r.Matches(description));

        return match?.Category;
    }
}
