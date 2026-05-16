using FreeBudget.Identity.Application.DTOs;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Identity.Application.Commands;

public sealed record RegisterUserCommand(
    string Email,
    string DisplayName,
    string Password) : IRequest<Result<UserDto>>;

internal sealed class RegisterUserHandler(
    IUserRepository userRepository,
    IUserCredentialRepository credentialRepository,
    IPasswordHasher passwordHasher)
    : IRequestHandler<RegisterUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return Result<UserDto>.Failure("Password must be at least 8 characters.");

        Email email;
        try
        {
            email = Email.Create(request.Email);
        }
        catch (ArgumentException ex)
        {
            return Result<UserDto>.Failure(ex.Message);
        }

        var existing = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
            return Result<UserDto>.Failure("An account with that email already exists.");

        User user;
        try
        {
            user = User.Create(email, request.DisplayName);
        }
        catch (ArgumentException ex)
        {
            return Result<UserDto>.Failure(ex.Message);
        }

        await userRepository.AddAsync(user, cancellationToken);

        var credential = UserCredential.Create(user.Id, passwordHasher.Hash(request.Password));
        await credentialRepository.AddAsync(credential, cancellationToken);

        return Result<UserDto>.Success(new UserDto(user.Id, user.Email.Value, user.DisplayName));
    }
}
