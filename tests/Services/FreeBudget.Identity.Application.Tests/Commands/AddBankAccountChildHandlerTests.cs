using FluentAssertions;
using FreeBudget.Identity.Application.Commands;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;
using NSubstitute;

namespace FreeBudget.Identity.Application.Tests.Commands;

public class AddBankAccountChildHandlerTests
{
    private readonly IBankAccountRepository _repo = Substitute.For<IBankAccountRepository>();
    private readonly AddBankAccountChildHandler _handler;
    private static readonly Guid UserId = Guid.NewGuid();

    public AddBankAccountChildHandlerTests()
    {
        _handler = new AddBankAccountChildHandler(_repo);
    }

    [Fact]
    public async Task Adds_child_to_existing_parent()
    {
        var parent = BankAccount.CreateParent(UserId, BankType.Wise, "Wise");
        _repo.GetByIdAsync(parent.Id, Arg.Any<CancellationToken>()).Returns(parent);
        _repo.GetChildrenAsync(parent.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<BankAccount>());

        var result = await _handler.Handle(
            new AddBankAccountChildCommand(parent.Id, "eur"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CurrencyCode.Should().Be("EUR");
        result.Value.ParentBankAccountId.Should().Be(parent.Id);
        await _repo.Received(1).AddAsync(
            Arg.Is<BankAccount>(b => b.ParentBankAccountId == parent.Id && b.CurrencyCode == "EUR"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_duplicate_currency_on_same_parent()
    {
        var parent = BankAccount.CreateParent(UserId, BankType.Wise, "Wise");
        var existingChild = BankAccount.CreateChild(parent, "GBP");
        _repo.GetByIdAsync(parent.Id, Arg.Any<CancellationToken>()).Returns(parent);
        _repo.GetChildrenAsync(parent.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { existingChild });

        var result = await _handler.Handle(
            new AddBankAccountChildCommand(parent.Id, "GBP"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().AddAsync(Arg.Any<BankAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_attaching_to_a_child_account()
    {
        var parent = BankAccount.CreateParent(UserId, BankType.Wise, "Wise");
        var child = BankAccount.CreateChild(parent, "GBP");
        _repo.GetByIdAsync(child.Id, Arg.Any<CancellationToken>()).Returns(child);

        var result = await _handler.Handle(
            new AddBankAccountChildCommand(child.Id, "USD"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Returns_failure_when_parent_not_found()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((BankAccount?)null);

        var result = await _handler.Handle(
            new AddBankAccountChildCommand(Guid.NewGuid(), "GBP"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
