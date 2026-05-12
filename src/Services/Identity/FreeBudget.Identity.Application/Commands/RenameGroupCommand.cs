using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Identity.Application.Commands;

public sealed record RenameGroupCommand(Guid GroupId, string Name) : IRequest<Result<bool>>;

internal sealed class RenameGroupHandler(IGroupRepository repository)
    : IRequestHandler<RenameGroupCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RenameGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await repository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group is null)
            return Result<bool>.Failure($"Group '{request.GroupId}' not found.");

        try
        {
            group.Rename(request.Name);
        }
        catch (ArgumentException ex)
        {
            return Result<bool>.Failure(ex.Message);
        }

        await repository.UpdateAsync(group, cancellationToken);
        return Result<bool>.Success(true);
    }
}
