using FreeBudget.Identity.Application.DTOs;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Identity.Application.Commands;

public sealed record AddGroupMemberCommand(
    Guid GroupId,
    string Label,
    Guid? OwningUserId) : IRequest<Result<GroupMemberDto>>;

internal sealed class AddGroupMemberHandler(IGroupRepository repository)
    : IRequestHandler<AddGroupMemberCommand, Result<GroupMemberDto>>
{
    public async Task<Result<GroupMemberDto>> Handle(AddGroupMemberCommand request, CancellationToken cancellationToken)
    {
        var group = await repository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group is null)
            return Result<GroupMemberDto>.Failure($"Group '{request.GroupId}' not found.");

        GroupMember member;
        try
        {
            member = group.AddMember(request.Label, request.OwningUserId);
        }
        catch (ArgumentException ex)
        {
            return Result<GroupMemberDto>.Failure(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result<GroupMemberDto>.Failure(ex.Message);
        }

        await repository.UpdateAsync(group, cancellationToken);
        return Result<GroupMemberDto>.Success(new GroupMemberDto(
            member.Id, member.GroupId, member.Label, member.OwningUserId, member.Role.ToString()));
    }
}
