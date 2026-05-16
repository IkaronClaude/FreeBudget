using FreeBudget.Identity.Application.DTOs;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.ValueObjects;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Identity.Application.Commands;

public sealed record VerifyCredentialsCommand(string Email, string Password) : IRequest<Result<UserDto>>;

internal sealed class VerifyCredentialsHandler(
    IUserRepository userRepository,
    IUserCredentialRepository credentialRepository,
    IPasswordHasher passwordHasher)
    : IRequestHandler<VerifyCredentialsCommand, Result<UserDto>>
{
    private const string InvalidCredentials = "Invalid email or password.";

    public async Task<Result<UserDto>> Handle(VerifyCredentialsCommand request, CancellationToken cancellationToken)
    {
        Email email;
        try
        {
            email = Email.Create(request.Email);
        }
        catch (ArgumentException)
        {
            return Result<UserDto>.Failure(InvalidCredentials);
        }

        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
            return Result<UserDto>.Failure(InvalidCredentials);

        var credential = await credentialRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (credential is null)
            return Result<UserDto>.Failure(InvalidCredentials);

        if (!passwordHasher.Verify(request.Password, credential.PasswordHash))
            return Result<UserDto>.Failure(InvalidCredentials);

        return Result<UserDto>.Success(new UserDto(user.Id, user.Email.Value, user.DisplayName));
    }
}
