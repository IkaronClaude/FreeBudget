using FreeBudget.Transactions.Domain.Entities;

namespace FreeBudget.Transactions.Application.Interfaces;

public interface ICategorizer
{
    string? Categorize(string description, IReadOnlyList<CategorizationRule> rules);
}
