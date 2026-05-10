using FluentAssertions;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Application.Queries;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;
using NSubstitute;

namespace FreeBudget.Identity.Application.Tests.Queries;

public class GetUserByEmailHandlerTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private readonly GetUserByEmailHandler _handler;

    public GetUserByEmailHandlerTests()
    {
        _handler = new GetUserByEmailHandler(_repo);
    }

    [Fact]
    public async Task Returns_dto_when_user_exists()
    {
        var user = User.Create(Email.Create("admin@freebudget.local"), "Admin");
        _repo.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _handler.Handle(
            new GetUserByEmailQuery("admin@freebudget.local"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("admin@freebudget.local");
        result.DisplayName.Should().Be("Admin");
    }

    [Fact]
    public async Task Returns_null_when_user_not_found()
    {
        _repo.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _handler.Handle(
            new GetUserByEmailQuery("nobody@freebudget.local"),
            CancellationToken.None);

        result.Should().BeNull();
    }
}
