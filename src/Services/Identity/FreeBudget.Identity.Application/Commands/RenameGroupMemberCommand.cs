using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Identity.Application.Commands;

public sealed record RenameGroupMemberCommand(Guid GroupId, Guid MemberId, string Label)
    : IRequest<Result<bool>>;

internal sealed class RenameGroupMemberHandler(IGroupRepository repository)
    : IRequestHandler<RenameGroupMemberCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RenameGroupMemberCommand request, CancellationToken cancellationToken)
    {
        var group = await repository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group is null)
            return Result<bool>.Failure($"Group '{request.GroupId}' not found.");

        var member = group.Members.FirstOrDefault(m => m.Id == request.MemberId);
        if (member is null)
            return Result<bool>.Failure($"Member '{request.MemberId}' is not in this group.");

        try
        {
            member.Rename(request.Label);
        }
        catch (ArgumentException ex)
        {
            return Result<bool>.Failure(ex.Message);
        }

        await repository.UpdateAsync(group, cancellationToken);
        return Result<bool>.Success(true);
    }
}
