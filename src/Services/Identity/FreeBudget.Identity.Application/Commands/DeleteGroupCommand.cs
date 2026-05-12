using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Identity.Application.Commands;

public sealed record DeleteGroupCommand(Guid GroupId) : IRequest<Result<bool>>;

internal sealed class DeleteGroupHandler(IGroupRepository repository)
    : IRequestHandler<DeleteGroupCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await repository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group is null)
            return Result<bool>.Failure($"Group '{request.GroupId}' not found.");

        await repository.DeleteAsync(group, cancellationToken);
        return Result<bool>.Success(true);
    }
}
