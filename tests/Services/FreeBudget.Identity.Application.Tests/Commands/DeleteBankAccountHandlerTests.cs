using FluentAssertions;
using FreeBudget.Identity.Application.Commands;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;
using NSubstitute;

namespace FreeBudget.Identity.Application.Tests.Commands;

public class DeleteBankAccountHandlerTests
{
    private readonly IBankAccountRepository _repo = Substitute.For<IBankAccountRepository>();
    private readonly DeleteBankAccountHandler _handler;
    private static readonly Guid UserId = Guid.NewGuid();

    public DeleteBankAccountHandlerTests()
    {
        _handler = new DeleteBankAccountHandler(_repo);
    }

    [Fact]
    public async Task Refuses_to_delete_a_parent_that_still_has_children()
    {
        var parent = BankAccount.CreateParent(UserId, BankType.Wise, "Wise");
        var child = BankAccount.CreateChild(parent, "GBP");
        _repo.GetByIdAsync(parent.Id, Arg.Any<CancellationToken>()).Returns(parent);
        _repo.GetChildrenAsync(parent.Id, Arg.Any<CancellationToken>()).Returns(new[] { child });

        var result = await _handler.Handle(new DeleteBankAccountCommand(parent.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<BankAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deletes_a_child_directly()
    {
        var parent = BankAccount.CreateParent(UserId, BankType.Wise, "Wise");
        var child = BankAccount.CreateChild(parent, "GBP");
        _repo.GetByIdAsync(child.Id, Arg.Any<CancellationToken>()).Returns(child);

        var result = await _handler.Handle(new DeleteBankAccountCommand(child.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).DeleteAsync(child, Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().GetChildrenAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deletes_a_standalone_account()
    {
        var account = BankAccount.Create(UserId, BankType.Barclays, "Personal");
        _repo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _repo.GetChildrenAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<BankAccount>());

        var result = await _handler.Handle(new DeleteBankAccountCommand(account.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).DeleteAsync(account, Arg.Any<CancellationToken>());
    }
}
