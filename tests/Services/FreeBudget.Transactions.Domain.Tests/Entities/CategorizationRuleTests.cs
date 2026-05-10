using FluentAssertions;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.Enums;

namespace FreeBudget.Transactions.Domain.Tests.Entities;

public class CategorizationRuleTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_sets_all_properties()
    {
        var rule = CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, "Groceries", 10);

        rule.Id.Should().NotBeEmpty();
        rule.CreatedByUserId.Should().Be(UserId);
        rule.Pattern.Should().Be("TESCO");
        rule.RuleMatchType.Should().Be(RuleMatchType.Contains);
        rule.Category.Should().Be("Groceries");
        rule.Priority.Should().Be(10);
    }

    [Fact]
    public void Create_trims_pattern_and_category()
    {
        var rule = CategorizationRule.Create(UserId, "  TESCO  ", RuleMatchType.Contains, "  Groceries  ");

        rule.Pattern.Should().Be("TESCO");
        rule.Category.Should().Be("Groceries");
    }

    [Fact]
    public void Create_defaults_priority_to_zero()
    {
        var rule = CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, "Groceries");

        rule.Priority.Should().Be(0);
    }

    [Fact]
    public void Create_with_empty_user_id_throws()
    {
        var act = () => CategorizationRule.Create(Guid.Empty, "TESCO", RuleMatchType.Contains, "Groceries");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_with_invalid_pattern_throws(string? pattern)
    {
        var act = () => CategorizationRule.Create(UserId, pattern!, RuleMatchType.Contains, "Groceries");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_with_invalid_category_throws(string? category)
    {
        var act = () => CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, category!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_changes_all_fields()
    {
        var rule = CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, "Groceries", 0);

        rule.Update("SAINSBURY", RuleMatchType.StartsWith, "Supermarket", 5);

        rule.Pattern.Should().Be("SAINSBURY");
        rule.RuleMatchType.Should().Be(RuleMatchType.StartsWith);
        rule.Category.Should().Be("Supermarket");
        rule.Priority.Should().Be(5);
    }

    [Fact]
    public void Update_trims_values()
    {
        var rule = CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, "Groceries");

        rule.Update("  ALDI  ", RuleMatchType.Exact, "  Shopping  ", 0);

        rule.Pattern.Should().Be("ALDI");
        rule.Category.Should().Be("Shopping");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Update_with_invalid_pattern_throws(string? pattern)
    {
        var rule = CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, "Groceries");

        var act = () => rule.Update(pattern!, RuleMatchType.Contains, "Groceries", 0);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Update_with_invalid_category_throws(string? category)
    {
        var rule = CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, "Groceries");

        var act = () => rule.Update("TESCO", RuleMatchType.Contains, category!, 0);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("PAYMENT TO TESCO STORES", true)]
    [InlineData("TESCO PETROL", true)]
    [InlineData("BIG TESCO", true)]
    [InlineData("SAINSBURY", false)]
    [InlineData("tesco lowercase", true)]
    public void Matches_Contains(string description, bool expected)
    {
        var rule = CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, "Groceries");

        rule.Matches(description).Should().Be(expected);
    }

    [Theory]
    [InlineData("TESCO", true)]
    [InlineData("tesco", true)]
    [InlineData("TESCO STORES", false)]
    [InlineData("BIG TESCO", false)]
    public void Matches_Exact(string description, bool expected)
    {
        var rule = CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Exact, "Groceries");

        rule.Matches(description).Should().Be(expected);
    }

    [Theory]
    [InlineData("TESCO STORES", true)]
    [InlineData("TESCO PETROL", true)]
    [InlineData("tesco something", true)]
    [InlineData("BIG TESCO", false)]
    public void Matches_StartsWith(string description, bool expected)
    {
        var rule = CategorizationRule.Create(UserId, "TESCO", RuleMatchType.StartsWith, "Groceries");

        rule.Matches(description).Should().Be(expected);
    }

    [Theory]
    [InlineData("BIG TESCO", true)]
    [InlineData("PAYMENT TESCO", true)]
    [InlineData("something tesco", true)]
    [InlineData("TESCO STORES", false)]
    public void Matches_EndsWith(string description, bool expected)
    {
        var rule = CategorizationRule.Create(UserId, "TESCO", RuleMatchType.EndsWith, "Groceries");

        rule.Matches(description).Should().Be(expected);
    }
}
