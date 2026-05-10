using FreeBudget.Identity.Application.DTOs;
using FreeBudget.Identity.Application.Interfaces;
using MediatR;

namespace FreeBudget.Identity.Application.Queries;

public sealed record GetUserGroupsQuery(Guid UserId) : IRequest<IReadOnlyList<GroupDto>>;

internal sealed class GetUserGroupsHandler(IGroupRepository repository)
    : IRequestHandler<GetUserGroupsQuery, IReadOnlyList<GroupDto>>
{
    public async Task<IReadOnlyList<GroupDto>> Handle(GetUserGroupsQuery request, CancellationToken cancellationToken)
    {
        var groups = await repository.GetByUserIdAsync(request.UserId, cancellationToken);
        return groups
            .Select(g => new GroupDto(
                g.Id,
                g.Name,
                g.Memberships.First(m => m.UserId == request.UserId).Role.ToString()))
            .ToList();
    }
}
