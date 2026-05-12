using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Identity.Application.Commands;

public sealed record RemoveGroupMemberCommand(Guid GroupId, Guid MemberId) : IRequest<Result<bool>>;

internal sealed class RemoveGroupMemberHandler(IGroupRepository repository)
    : IRequestHandler<RemoveGroupMemberCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RemoveGroupMemberCommand request, CancellationToken cancellationToken)
    {
        var group = await repository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group is null)
            return Result<bool>.Failure($"Group '{request.GroupId}' not found.");

        try
        {
            group.RemoveMember(request.MemberId);
        }
        catch (InvalidOperationException ex)
        {
            return Result<bool>.Failure(ex.Message);
        }

        await repository.UpdateAsync(group, cancellationToken);
        return Result<bool>.Success(true);
    }
}
