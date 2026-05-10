using FluentAssertions;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.Enums;
using FreeBudget.Transactions.Infrastructure.Categorization;

namespace FreeBudget.Transactions.Infrastructure.Tests.Categorization;

public class RuleCategorizerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly RuleCategorizer _categorizer = new();

    [Fact]
    public void Returns_null_when_no_rules()
    {
        var result = _categorizer.Categorize("TESCO STORES", []);

        result.Should().BeNull();
    }

    [Fact]
    public void Returns_null_when_no_rules_match()
    {
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create(UserId, "SAINSBURY", RuleMatchType.Contains, "Groceries"),
        };

        var result = _categorizer.Categorize("TESCO STORES", rules);

        result.Should().BeNull();
    }

    [Fact]
    public void Returns_category_for_matching_rule()
    {
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, "Groceries"),
        };

        var result = _categorizer.Categorize("PAYMENT TO TESCO STORES", rules);

        result.Should().Be("Groceries");
    }

    [Fact]
    public void Higher_priority_rule_wins()
    {
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, "Groceries", 0),
            CategorizationRule.Create(UserId, "TESCO PETROL", RuleMatchType.Contains, "Fuel", 10),
        };

        var result = _categorizer.Categorize("TESCO PETROL STATION", rules);

        result.Should().Be("Fuel");
    }

    [Fact]
    public void First_matching_rule_at_same_priority_wins()
    {
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, "Groceries", 0),
            CategorizationRule.Create(UserId, "STORE", RuleMatchType.Contains, "Shopping", 0),
        };

        var result = _categorizer.Categorize("TESCO STORE", rules);

        result.Should().NotBeNull();
    }

    [Fact]
    public void Exact_match_with_different_case()
    {
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create(UserId, "NETFLIX", RuleMatchType.Exact, "Entertainment"),
        };

        var result = _categorizer.Categorize("netflix", rules);

        result.Should().Be("Entertainment");
    }

    [Fact]
    public void Multiple_rules_first_match_by_priority()
    {
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create(UserId, "AMZN", RuleMatchType.StartsWith, "Shopping", 5),
            CategorizationRule.Create(UserId, "AMZN MKTP", RuleMatchType.StartsWith, "Amazon", 10),
            CategorizationRule.Create(UserId, "AMZN PRIME", RuleMatchType.Exact, "Subscriptions", 20),
        };

        _categorizer.Categorize("AMZN PRIME", rules).Should().Be("Subscriptions");
        _categorizer.Categorize("AMZN MKTP GB", rules).Should().Be("Amazon");
        _categorizer.Categorize("AMZN FRESH", rules).Should().Be("Shopping");
        _categorizer.Categorize("TESCO", rules).Should().BeNull();
    }
}
