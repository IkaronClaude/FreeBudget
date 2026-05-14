using FluentAssertions;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.Enums;

namespace FreeBudget.Transactions.Domain.Tests.Entities;

public class SharingRuleTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid Payer = Guid.NewGuid();
    private static readonly Guid Other = Guid.NewGuid();

    [Fact]
    public void Create_expense_rule_validates_payer_and_participants()
    {
        var act = () => SharingRule.Create(UserId, "TESCO", RuleMatchType.Contains,
            LedgerEntryKind.Expense, GroupId, Guid.Empty, [Payer, Other]);

        act.Should().Throw<ArgumentException>().WithMessage("*Payer*");
    }

    [Fact]
    public void Exclude_rule_does_not_require_payer_or_participants()
    {
        var rule = SharingRule.Create(UserId, "GROCERY", RuleMatchType.Contains,
            LedgerEntryKind.Exclude, GroupId, Guid.Empty, []);

        rule.EntryType.Should().Be(LedgerEntryKind.Exclude);
        rule.PaidByMemberId.Should().Be(Guid.Empty);
        rule.ParticipantMemberIds.Should().BeEmpty();
    }

    [Fact]
    public void Any_match_type_does_not_require_a_pattern()
    {
        var rule = SharingRule.Create(UserId, "", RuleMatchType.Any,
            LedgerEntryKind.Exclude, GroupId, Guid.Empty, []);

        rule.RuleMatchType.Should().Be(RuleMatchType.Any);
        rule.Pattern.Should().BeEmpty();
    }

    [Fact]
    public void Non_any_match_still_requires_a_pattern()
    {
        var act = () => SharingRule.Create(UserId, "", RuleMatchType.Contains,
            LedgerEntryKind.Exclude, GroupId, Guid.Empty, []);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("anything", true)]
    [InlineData("", true)]
    public void Any_match_returns_true_regardless_of_description(string description, bool expected)
    {
        var rule = SharingRule.Create(UserId, "", RuleMatchType.Any,
            LedgerEntryKind.Exclude, GroupId, Guid.Empty, []);

        rule.Matches(description).Should().Be(expected);
    }

    [Fact]
    public void Group_id_is_still_required_for_exclude_rules()
    {
        var act = () => SharingRule.Create(UserId, "", RuleMatchType.Any,
            LedgerEntryKind.Exclude, Guid.Empty, Guid.Empty, []);

        act.Should().Throw<ArgumentException>().WithMessage("*Group ID*");
    }

    [Fact]
    public void Update_allows_switching_from_expense_to_exclude()
    {
        var rule = SharingRule.Create(UserId, "TESCO", RuleMatchType.Contains,
            LedgerEntryKind.Expense, GroupId, Payer, [Payer, Other]);

        rule.Update("", RuleMatchType.Any, LedgerEntryKind.Exclude,
            GroupId, Guid.Empty, [], 0);

        rule.EntryType.Should().Be(LedgerEntryKind.Exclude);
        rule.RuleMatchType.Should().Be(RuleMatchType.Any);
        rule.ParticipantMemberIds.Should().BeEmpty();
    }

    [Fact]
    public void Contains_match_is_case_insensitive()
    {
        var rule = SharingRule.Create(UserId, "tesco", RuleMatchType.Contains,
            LedgerEntryKind.Expense, GroupId, Payer, [Payer, Other]);

        rule.Matches("Card payment TESCO 1234").Should().BeTrue();
    }
}
