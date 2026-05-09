using FluentAssertions;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;
using FreeBudget.Identity.Infrastructure.Persistence;
using FreeBudget.Identity.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Identity.Infrastructure.Tests.Persistence.Repositories;

public class UserRepositoryTests
{
    private static DbContextOptions<IdentityDbContext> CreateOptions()
        => new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task AddAsync_persists_user()
    {
        var options = CreateOptions();
        await using (var context = new IdentityDbContext(options))
        {
            var repo = new UserRepository(context);
            var user = User.Create(Email.Create("test@example.com"), "Test User");
            await repo.AddAsync(user);
        }

        await using (var context = new IdentityDbContext(options))
        {
            var found = await context.Users.FirstAsync();
            found.DisplayName.Should().Be("Test User");
        }
    }

    [Fact]
    public async Task GetByIdAsync_returns_user_when_exists()
    {
        var options = CreateOptions();
        var userId = Guid.Empty;

        await using (var context = new IdentityDbContext(options))
        {
            var user = User.Create(Email.Create("test@example.com"), "Test User");
            userId = user.Id;
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
        }

        await using (var context = new IdentityDbContext(options))
        {
            var repo = new UserRepository(context);
            var found = await repo.GetByIdAsync(userId);
            found.Should().NotBeNull();
            found!.Id.Should().Be(userId);
        }
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_not_found()
    {
        var options = CreateOptions();
        await using var context = new IdentityDbContext(options);
        var repo = new UserRepository(context);

        var found = await repo.GetByIdAsync(Guid.NewGuid());

        found.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_returns_user_when_exists()
    {
        var options = CreateOptions();
        var email = Email.Create("find@example.com");

        await using (var context = new IdentityDbContext(options))
        {
            var user = User.Create(email, "Find Me");
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
        }

        await using (var context = new IdentityDbContext(options))
        {
            var repo = new UserRepository(context);
            var found = await repo.GetByEmailAsync(email);
            found.Should().NotBeNull();
            found!.Email.Should().Be(email);
        }
    }

    [Fact]
    public async Task UpdateAsync_persists_changes()
    {
        var options = CreateOptions();
        var userId = Guid.Empty;

        await using (var context = new IdentityDbContext(options))
        {
            var user = User.Create(Email.Create("test@example.com"), "Original");
            userId = user.Id;
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
        }

        await using (var context = new IdentityDbContext(options))
        {
            var user = await context.Users.FindAsync(userId);
            user!.UpdateDisplayName("Updated");
            var repo = new UserRepository(context);
            await repo.UpdateAsync(user);
        }

        await using (var context = new IdentityDbContext(options))
        {
            var found = await context.Users.FindAsync(userId);
            found!.DisplayName.Should().Be("Updated");
        }
    }
}
