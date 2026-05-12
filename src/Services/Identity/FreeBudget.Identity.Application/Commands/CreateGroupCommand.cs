using FreeBudget.Identity.Application.DTOs;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Application.Queries;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Identity.Application.Commands;

public sealed record CreateGroupCommand(
    string Name,
    Guid CreatedByUserId,
    string CreatorLabel) : IRequest<Result<GroupDto>>;

internal sealed class CreateGroupHandler(IGroupRepository repository)
    : IRequestHandler<CreateGroupCommand, Result<GroupDto>>
{
    public async Task<Result<GroupDto>> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        Group group;
        try
        {
            group = Group.Create(request.Name, request.CreatedByUserId, request.CreatorLabel);
        }
        catch (ArgumentException ex)
        {
            return Result<GroupDto>.Failure(ex.Message);
        }

        await repository.AddAsync(group, cancellationToken);
        return Result<GroupDto>.Success(GetUserGroupsHandler.MapToDto(group));
    }
}
