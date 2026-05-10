using FreeBudget.Identity.Application.DTOs;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.ValueObjects;
using MediatR;

namespace FreeBudget.Identity.Application.Queries;

public sealed record GetUserByEmailQuery(string Email) : IRequest<UserDto?>;

internal sealed class GetUserByEmailHandler(IUserRepository repository)
    : IRequestHandler<GetUserByEmailQuery, UserDto?>
{
    public async Task<UserDto?> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);
        var user = await repository.GetByEmailAsync(email, cancellationToken);
        return user is null ? null : new UserDto(user.Id, user.Email.Value, user.DisplayName);
    }
}
