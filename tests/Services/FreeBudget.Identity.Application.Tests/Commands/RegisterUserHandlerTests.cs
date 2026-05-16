using FluentAssertions;
using FreeBudget.Identity.Application.Commands;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;
using NSubstitute;

namespace FreeBudget.Identity.Application.Tests.Commands;

public class RegisterUserHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUserCredentialRepository _credentials = Substitute.For<IUserCredentialRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _handler = new RegisterUserHandler(_users, _credentials, _hasher);
        _hasher.Hash(Arg.Any<string>()).Returns(c => $"hashed:{c.Arg<string>()}");
    }

    [Fact]
    public async Task Creates_user_and_credential_on_success()
    {
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.Handle(
            new RegisterUserCommand("anna@example.com", "Anna", "hunter2pw"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("anna@example.com");
        result.Value.DisplayName.Should().Be("Anna");
        await _users.Received(1).AddAsync(Arg.Is<User>(u => u.Email.Value == "anna@example.com"), Arg.Any<CancellationToken>());
        await _credentials.Received(1).AddAsync(
            Arg.Is<UserCredential>(c => c.PasswordHash == "hashed:hunter2pw"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_email_already_taken()
    {
        var existing = User.Create(Email.Create("anna@example.com"), "Anna");
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.Handle(
            new RegisterUserCommand("anna@example.com", "Anna", "hunter2pw"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _credentials.DidNotReceive().AddAsync(Arg.Any<UserCredential>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short")]
    public async Task Fails_when_password_too_short(string password)
    {
        var result = await _handler.Handle(
            new RegisterUserCommand("anna@example.com", "Anna", password),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("8 characters");
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_email_invalid()
    {
        var result = await _handler.Handle(
            new RegisterUserCommand("not-an-email", "Anna", "hunter2pw"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_display_name_empty()
    {
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.Handle(
            new RegisterUserCommand("anna@example.com", "   ", "hunter2pw"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
