using FreeBudget.Identity.Application.DTOs;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Identity.Application.Commands;

public sealed record LinkGroupMemberToUserCommand(Guid GroupId, Guid MemberId, Guid OwningUserId)
    : IRequest<Result<GroupMemberDto>>;

internal sealed class LinkGroupMemberToUserHandler(
    IGroupRepository groupRepository,
    IUserRepository userRepository)
    : IRequestHandler<LinkGroupMemberToUserCommand, Result<GroupMemberDto>>
{
    public async Task<Result<GroupMemberDto>> Handle(LinkGroupMemberToUserCommand request, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group is null)
            return Result<GroupMemberDto>.Failure($"Group '{request.GroupId}' not found.");

        var user = await userRepository.GetByIdAsync(request.OwningUserId, cancellationToken);
        if (user is null)
            return Result<GroupMemberDto>.Failure($"User '{request.OwningUserId}' not found.");

        try
        {
            var member = group.LinkMemberToUser(request.MemberId, request.OwningUserId);
            await groupRepository.UpdateAsync(group, cancellationToken);
            return Result<GroupMemberDto>.Success(new GroupMemberDto(
                member.Id, member.GroupId, member.Label, member.OwningUserId, member.Role.ToString()));
        }
        catch (ArgumentException ex)
        {
            return Result<GroupMemberDto>.Failure(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result<GroupMemberDto>.Failure(ex.Message);
        }
    }
}
