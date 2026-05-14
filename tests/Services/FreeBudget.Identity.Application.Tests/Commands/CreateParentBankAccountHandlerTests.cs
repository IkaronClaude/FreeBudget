using FluentAssertions;
using FreeBudget.Identity.Application.Commands;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.Entities;
using NSubstitute;

namespace FreeBudget.Identity.Application.Tests.Commands;

public class CreateParentBankAccountHandlerTests
{
    private readonly IBankAccountRepository _repo = Substitute.For<IBankAccountRepository>();
    private readonly CreateParentBankAccountHandler _handler;
    private static readonly Guid UserId = Guid.NewGuid();

    public CreateParentBankAccountHandlerTests()
    {
        _handler = new CreateParentBankAccountHandler(_repo);
    }

    [Fact]
    public async Task Saves_parent_and_one_child_per_currency()
    {
        List<BankAccount>? saved = null;
        await _repo.AddRangeAsync(
            Arg.Do<IEnumerable<BankAccount>>(items => saved = items.ToList()),
            Arg.Any<CancellationToken>());

        var result = await _handler.Handle(
            new CreateParentBankAccountCommand(UserId, "Wise", "Wise", new[] { "GBP", "USD", "EUR" }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        saved.Should().HaveCount(4);
        var parent = saved!.Single(a => a.ParentBankAccountId is null);
        parent.Nickname.Should().Be("Wise");
        parent.CurrencyCode.Should().BeNull();
        var children = saved.Where(a => a.ParentBankAccountId == parent.Id).ToList();
        children.Should().HaveCount(3);
        children.Select(c => c.CurrencyCode).Should().BeEquivalentTo(new[] { "GBP", "USD", "EUR" });
        result.Value!.Id.Should().Be(parent.Id);
    }

    [Fact]
    public async Task Deduplicates_currencies_case_insensitively()
    {
        List<BankAccount>? saved = null;
        await _repo.AddRangeAsync(
            Arg.Do<IEnumerable<BankAccount>>(items => saved = items.ToList()),
            Arg.Any<CancellationToken>());

        var result = await _handler.Handle(
            new CreateParentBankAccountCommand(UserId, "Wise", "Wise", new[] { "gbp", "GBP", "USD" }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var children = saved!.Where(a => a.ParentBankAccountId is not null).ToList();
        children.Select(c => c.CurrencyCode).Should().BeEquivalentTo(new[] { "GBP", "USD" });
    }

    [Fact]
    public async Task Empty_currency_list_returns_failure()
    {
        var result = await _handler.Handle(
            new CreateParentBankAccountCommand(UserId, "Wise", "Wise", Array.Empty<string>()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<BankAccount>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_bank_type_returns_failure()
    {
        var result = await _handler.Handle(
            new CreateParentBankAccountCommand(UserId, "BogusBank", "Whatever", new[] { "GBP" }),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<BankAccount>>(), Arg.Any<CancellationToken>());
    }
}
