using FluentAssertions;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Application.Queries;
using FreeBudget.Identity.Domain.Entities;
using NSubstitute;

namespace FreeBudget.Identity.Application.Tests.Queries;

public class GetUserGroupsHandlerTests
{
    private readonly IGroupRepository _repo = Substitute.For<IGroupRepository>();
    private readonly GetUserGroupsHandler _handler;

    public GetUserGroupsHandlerTests()
    {
        _handler = new GetUserGroupsHandler(_repo);
    }

    [Fact]
    public async Task Returns_groups_with_member_role()
    {
        var userId = Guid.NewGuid();
        var personal = Group.Create("Personal", userId);

        var otherCreator = Guid.NewGuid();
        var shared = Group.Create("Shared", otherCreator);
        shared.AddMember(userId);

        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[] { personal, shared });

        var result = await _handler.Handle(new GetUserGroupsQuery(userId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.First(g => g.Name == "Personal").Role.Should().Be("Admin");
        result.First(g => g.Name == "Shared").Role.Should().Be("Member");
    }

    [Fact]
    public async Task Returns_empty_when_user_is_not_in_any_group()
    {
        _repo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Group>());

        var result = await _handler.Handle(new GetUserGroupsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
