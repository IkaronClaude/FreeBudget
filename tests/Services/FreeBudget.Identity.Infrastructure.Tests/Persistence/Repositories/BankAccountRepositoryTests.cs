using FluentAssertions;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;
using FreeBudget.Identity.Infrastructure.Persistence;
using FreeBudget.Identity.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Identity.Infrastructure.Tests.Persistence.Repositories;

public class BankAccountRepositoryTests
{
    private static DbContextOptions<IdentityDbContext> CreateOptions()
        => new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task AddAsync_persists_bank_account()
    {
        var options = CreateOptions();
        Guid accountId;

        await using (var context = new IdentityDbContext(options))
        {
            var repo = new BankAccountRepository(context);
            var account = BankAccount.Create(Guid.NewGuid(), BankType.Barclays, "Main Account");
            accountId = account.Id;
            await repo.AddAsync(account);
        }

        await using (var context = new IdentityDbContext(options))
        {
            var found = await context.BankAccounts.FindAsync(accountId);
            found.Should().NotBeNull();
            found!.Nickname.Should().Be("Main Account");
        }
    }

    [Fact]
    public async Task GetByIdAsync_includes_access_grants()
    {
        var options = CreateOptions();
        Guid accountId;

        await using (var context = new IdentityDbContext(options))
        {
            var account = BankAccount.Create(Guid.NewGuid(), BankType.Barclays, "Account");
            account.GrantAccessToGroup(Guid.NewGuid());
            accountId = account.Id;
            await context.BankAccounts.AddAsync(account);
            await context.SaveChangesAsync();
        }

        await using (var context = new IdentityDbContext(options))
        {
            var repo = new BankAccountRepository(context);
            var found = await repo.GetByIdAsync(accountId);

            found.Should().NotBeNull();
            found!.AccessGrants.Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task GetByOwnerUserIdAsync_returns_accounts_for_owner()
    {
        var options = CreateOptions();
        var ownerId = Guid.NewGuid();

        await using (var context = new IdentityDbContext(options))
        {
            var account1 = BankAccount.Create(ownerId, BankType.Barclays, "Barclays");
            var account2 = BankAccount.Create(ownerId, BankType.Wise, "Wise");
            var otherAccount = BankAccount.Create(Guid.NewGuid(), BankType.NatWest, "NatWest");
            await context.BankAccounts.AddRangeAsync(account1, account2, otherAccount);
            await context.SaveChangesAsync();
        }

        await using (var context = new IdentityDbContext(options))
        {
            var repo = new BankAccountRepository(context);
            var accounts = await repo.GetByOwnerUserIdAsync(ownerId);

            accounts.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task GetByGroupAccessAsync_returns_accounts_with_group_access()
    {
        var options = CreateOptions();
        var groupId = Guid.NewGuid();

        await using (var context = new IdentityDbContext(options))
        {
            var account1 = BankAccount.Create(Guid.NewGuid(), BankType.Barclays, "Shared");
            account1.GrantAccessToGroup(groupId);
            var account2 = BankAccount.Create(Guid.NewGuid(), BankType.Wise, "Not Shared");
            await context.BankAccounts.AddRangeAsync(account1, account2);
            await context.SaveChangesAsync();
        }

        await using (var context = new IdentityDbContext(options))
        {
            var repo = new BankAccountRepository(context);
            var accounts = await repo.GetByGroupAccessAsync(groupId);

            accounts.Should().ContainSingle();
            accounts[0].Nickname.Should().Be("Shared");
        }
    }
}
