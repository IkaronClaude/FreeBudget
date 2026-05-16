using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Infrastructure.Persistence;
using FreeBudget.Identity.Infrastructure.Persistence.Repositories;
using FreeBudget.Identity.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBudget.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("IdentityDb"))
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IBankAccountRepository, BankAccountRepository>();
        services.AddScoped<IUserCredentialRepository, UserCredentialRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        return services;
    }
}
