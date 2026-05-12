using FreeBudget.Identity.Application.DTOs;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.Entities;
using MediatR;

namespace FreeBudget.Identity.Application.Queries;

public sealed record GetUserGroupsQuery(Guid UserId) : IRequest<IReadOnlyList<GroupDto>>;

internal sealed class GetUserGroupsHandler(IGroupRepository repository)
    : IRequestHandler<GetUserGroupsQuery, IReadOnlyList<GroupDto>>
{
    public async Task<IReadOnlyList<GroupDto>> Handle(GetUserGroupsQuery request, CancellationToken cancellationToken)
    {
        var groups = await repository.GetByUserIdAsync(request.UserId, cancellationToken);
        return groups.Select(MapToDto).ToList();
    }

    internal static GroupDto MapToDto(Group g) => new(
        g.Id,
        g.Name,
        g.CreatedByUserId,
        g.Members.Select(m => new GroupMemberDto(
            m.Id,
            m.GroupId,
            m.Label,
            m.OwningUserId,
            m.Role.ToString())).ToList());
}
