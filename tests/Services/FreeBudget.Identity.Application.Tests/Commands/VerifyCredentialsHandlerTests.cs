using FluentAssertions;
using FreeBudget.Identity.Application.Commands;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;
using NSubstitute;

namespace FreeBudget.Identity.Application.Tests.Commands;

public class VerifyCredentialsHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUserCredentialRepository _credentials = Substitute.For<IUserCredentialRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly VerifyCredentialsHandler _handler;

    public VerifyCredentialsHandlerTests()
    {
        _handler = new VerifyCredentialsHandler(_users, _credentials, _hasher);
    }

    [Fact]
    public async Task Returns_user_when_password_matches()
    {
        var user = User.Create(Email.Create("anna@example.com"), "Anna");
        var credential = UserCredential.Create(user.Id, "stored-hash");
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _credentials.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(credential);
        _hasher.Verify("hunter2pw", "stored-hash").Returns(true);

        var result = await _handler.Handle(
            new VerifyCredentialsCommand("anna@example.com", "hunter2pw"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(user.Id);
        result.Value.DisplayName.Should().Be("Anna");
    }

    [Fact]
    public async Task Fails_when_password_wrong()
    {
        var user = User.Create(Email.Create("anna@example.com"), "Anna");
        var credential = UserCredential.Create(user.Id, "stored-hash");
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _credentials.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(credential);
        _hasher.Verify("wrong-pw", "stored-hash").Returns(false);

        var result = await _handler.Handle(
            new VerifyCredentialsCommand("anna@example.com", "wrong-pw"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Fails_when_user_not_found()
    {
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.Handle(
            new VerifyCredentialsCommand("ghost@example.com", "hunter2pw"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Fails_when_no_credential_exists()
    {
        var user = User.Create(Email.Create("anna@example.com"), "Anna");
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _credentials.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns((UserCredential?)null);

        var result = await _handler.Handle(
            new VerifyCredentialsCommand("anna@example.com", "hunter2pw"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Fails_quietly_when_email_invalid()
    {
        var result = await _handler.Handle(
            new VerifyCredentialsCommand("not-an-email", "hunter2pw"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email or password.");
        await _users.DidNotReceive().GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>());
    }
}
