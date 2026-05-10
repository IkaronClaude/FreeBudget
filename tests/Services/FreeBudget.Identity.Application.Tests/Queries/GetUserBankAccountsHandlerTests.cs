using FluentAssertions;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Application.Queries;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;
using NSubstitute;

namespace FreeBudget.Identity.Application.Tests.Queries;

public class GetUserBankAccountsHandlerTests
{
    private readonly IBankAccountRepository _repo = Substitute.For<IBankAccountRepository>();
    private readonly GetUserBankAccountsHandler _handler;

    public GetUserBankAccountsHandlerTests()
    {
        _handler = new GetUserBankAccountsHandler(_repo);
    }

    [Fact]
    public async Task Maps_bank_account_fields_to_dto()
    {
        var userId = Guid.NewGuid();
        var account = BankAccount.Create(userId, BankType.Barclays, "Barclays - Personal");

        _repo.GetByOwnerUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[] { account });

        var result = await _handler.Handle(new GetUserBankAccountsQuery(userId), CancellationToken.None);

        result.Should().ContainSingle();
        var dto = result[0];
        dto.Id.Should().Be(account.Id);
        dto.OwnerUserId.Should().Be(userId);
        dto.BankType.Should().Be("Barclays");
        dto.Nickname.Should().Be("Barclays - Personal");
        dto.HasApiCredentials.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_empty_when_user_has_no_accounts()
    {
        _repo.GetByOwnerUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<BankAccount>());

        var result = await _handler.Handle(new GetUserBankAccountsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
