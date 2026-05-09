using FluentAssertions;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Infrastructure.Persistence;
using FreeBudget.Identity.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Identity.Infrastructure.Tests.Persistence.Repositories;

public class GroupRepositoryTests
{
    private static DbContextOptions<IdentityDbContext> CreateOptions()
        => new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task AddAsync_persists_group_with_memberships()
    {
        var options = CreateOptions();
        Guid groupId;

        await using (var context = new IdentityDbContext(options))
        {
            var repo = new GroupRepository(context);
            var group = Group.Create("Household", Guid.NewGuid());
            groupId = group.Id;
            await repo.AddAsync(group);
        }

        await using (var context = new IdentityDbContext(options))
        {
            var found = await context.Groups
                .Include(g => g.Memberships)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            found.Should().NotBeNull();
            found!.Name.Should().Be("Household");
            found.Memberships.Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task GetByIdAsync_includes_memberships()
    {
        var options = CreateOptions();
        var creatorId = Guid.NewGuid();
        Guid groupId;

        await using (var context = new IdentityDbContext(options))
        {
            var group = Group.Create("Test Group", creatorId);
            group.AddMember(Guid.NewGuid());
            groupId = group.Id;
            await context.Groups.AddAsync(group);
            await context.SaveChangesAsync();
        }

        await using (var context = new IdentityDbContext(options))
        {
            var repo = new GroupRepository(context);
            var found = await repo.GetByIdAsync(groupId);

            found.Should().NotBeNull();
            found!.Memberships.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task GetByUserIdAsync_returns_groups_containing_user()
    {
        var options = CreateOptions();
        var userId = Guid.NewGuid();

        await using (var context = new IdentityDbContext(options))
        {
            var group1 = Group.Create("Group 1", userId);
            var group2 = Group.Create("Group 2", Guid.NewGuid());
            group2.AddMember(userId);
            var group3 = Group.Create("Group 3", Guid.NewGuid());
            await context.Groups.AddRangeAsync(group1, group2, group3);
            await context.SaveChangesAsync();
        }

        await using (var context = new IdentityDbContext(options))
        {
            var repo = new GroupRepository(context);
            var groups = await repo.GetByUserIdAsync(userId);

            groups.Should().HaveCount(2);
            groups.Select(g => g.Name).Should().Contain("Group 1").And.Contain("Group 2");
        }
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_not_found()
    {
        var options = CreateOptions();
        await using var context = new IdentityDbContext(options);
        var repo = new GroupRepository(context);

        var found = await repo.GetByIdAsync(Guid.NewGuid());

        found.Should().BeNull();
    }
}
