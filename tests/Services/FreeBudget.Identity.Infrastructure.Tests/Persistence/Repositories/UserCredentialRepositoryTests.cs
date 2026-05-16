using FluentAssertions;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Infrastructure.Persistence;
using FreeBudget.Identity.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Identity.Infrastructure.Tests.Persistence.Repositories;

public class UserCredentialRepositoryTests
{
    private static DbContextOptions<IdentityDbContext> CreateOptions()
        => new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task AddAsync_persists_credential()
    {
        var options = CreateOptions();
        var userId = Guid.NewGuid();

        await using (var context = new IdentityDbContext(options))
        {
            var repo = new UserCredentialRepository(context);
            await repo.AddAsync(UserCredential.Create(userId, "hash-1"));
        }

        await using (var context = new IdentityDbContext(options))
        {
            var found = await context.UserCredentials.FirstAsync();
            found.UserId.Should().Be(userId);
            found.PasswordHash.Should().Be("hash-1");
        }
    }

    [Fact]
    public async Task GetByUserIdAsync_returns_credential_when_exists()
    {
        var options = CreateOptions();
        var userId = Guid.NewGuid();

        await using (var context = new IdentityDbContext(options))
        {
            await context.UserCredentials.AddAsync(UserCredential.Create(userId, "hash-2"));
            await context.SaveChangesAsync();
        }

        await using (var context = new IdentityDbContext(options))
        {
            var repo = new UserCredentialRepository(context);
            var found = await repo.GetByUserIdAsync(userId);
            found.Should().NotBeNull();
            found!.PasswordHash.Should().Be("hash-2");
        }
    }

    [Fact]
    public async Task GetByUserIdAsync_returns_null_when_missing()
    {
        var options = CreateOptions();
        await using var context = new IdentityDbContext(options);
        var repo = new UserCredentialRepository(context);

        var found = await repo.GetByUserIdAsync(Guid.NewGuid());

        found.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_persists_new_hash()
    {
        var options = CreateOptions();
        var userId = Guid.NewGuid();
        Guid credentialId;

        await using (var context = new IdentityDbContext(options))
        {
            var credential = UserCredential.Create(userId, "old-hash");
            credentialId = credential.Id;
            await context.UserCredentials.AddAsync(credential);
            await context.SaveChangesAsync();
        }

        await using (var context = new IdentityDbContext(options))
        {
            var credential = await context.UserCredentials.FindAsync(credentialId);
            credential!.UpdateHash("new-hash");
            var repo = new UserCredentialRepository(context);
            await repo.UpdateAsync(credential);
        }

        await using (var context = new IdentityDbContext(options))
        {
            var found = await context.UserCredentials.FindAsync(credentialId);
            found!.PasswordHash.Should().Be("new-hash");
        }
    }
}
