using FluentAssertions;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.Enums;
using FreeBudget.Transactions.Infrastructure.Persistence;
using FreeBudget.Transactions.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Transactions.Infrastructure.Tests.Persistence.Repositories;

public class CategorizationRuleRepositoryTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static DbContextOptions<TransactionsDbContext> CreateOptions()
        => new DbContextOptionsBuilder<TransactionsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task AddAsync_persists_rule()
    {
        var options = CreateOptions();
        Guid ruleId;

        await using (var context = new TransactionsDbContext(options))
        {
            var repo = new CategorizationRuleRepository(context);
            var rule = CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, "Groceries", 10);
            ruleId = rule.Id;
            await repo.AddAsync(rule);
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var found = await context.CategorizationRules.FirstOrDefaultAsync(r => r.Id == ruleId);
            found.Should().NotBeNull();
            found!.Pattern.Should().Be("TESCO");
            found.RuleMatchType.Should().Be(RuleMatchType.Contains);
            found.Category.Should().Be("Groceries");
            found.Priority.Should().Be(10);
        }
    }

    [Fact]
    public async Task GetByIdAsync_returns_rule()
    {
        var options = CreateOptions();
        var rule = CategorizationRule.Create(UserId, "NETFLIX", RuleMatchType.Exact, "Entertainment");

        await using (var context = new TransactionsDbContext(options))
        {
            await context.CategorizationRules.AddAsync(rule);
            await context.SaveChangesAsync();
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var repo = new CategorizationRuleRepository(context);
            var found = await repo.GetByIdAsync(rule.Id);
            found.Should().NotBeNull();
            found!.Pattern.Should().Be("NETFLIX");
        }
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_not_found()
    {
        var options = CreateOptions();

        await using var context = new TransactionsDbContext(options);
        var repo = new CategorizationRuleRepository(context);
        var result = await repo.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserIdAsync_returns_rules_ordered_by_priority()
    {
        var options = CreateOptions();

        await using (var context = new TransactionsDbContext(options))
        {
            await context.CategorizationRules.AddRangeAsync(
                CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, "Groceries", 0),
                CategorizationRule.Create(UserId, "AMZN", RuleMatchType.StartsWith, "Shopping", 10),
                CategorizationRule.Create(Guid.NewGuid(), "OTHER", RuleMatchType.Exact, "Other"));
            await context.SaveChangesAsync();
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var repo = new CategorizationRuleRepository(context);
            var rules = await repo.GetByUserIdAsync(UserId);
            rules.Should().HaveCount(2);
            rules[0].Pattern.Should().Be("AMZN");
            rules[1].Pattern.Should().Be("TESCO");
        }
    }

    [Fact]
    public async Task UpdateAsync_persists_changes()
    {
        var options = CreateOptions();
        var rule = CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, "Groceries");

        await using (var context = new TransactionsDbContext(options))
        {
            await context.CategorizationRules.AddAsync(rule);
            await context.SaveChangesAsync();
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var repo = new CategorizationRuleRepository(context);
            var found = await repo.GetByIdAsync(rule.Id);
            found!.Update("SAINSBURY", RuleMatchType.StartsWith, "Supermarket", 5);
            await repo.UpdateAsync(found);
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var found = await context.CategorizationRules.FirstAsync(r => r.Id == rule.Id);
            found.Pattern.Should().Be("SAINSBURY");
            found.RuleMatchType.Should().Be(RuleMatchType.StartsWith);
            found.Category.Should().Be("Supermarket");
            found.Priority.Should().Be(5);
        }
    }

    [Fact]
    public async Task DeleteAsync_removes_rule()
    {
        var options = CreateOptions();
        var rule = CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, "Groceries");

        await using (var context = new TransactionsDbContext(options))
        {
            await context.CategorizationRules.AddAsync(rule);
            await context.SaveChangesAsync();
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var repo = new CategorizationRuleRepository(context);
            var found = await repo.GetByIdAsync(rule.Id);
            await repo.DeleteAsync(found!);
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var exists = await context.CategorizationRules.AnyAsync(r => r.Id == rule.Id);
            exists.Should().BeFalse();
        }
    }
}
