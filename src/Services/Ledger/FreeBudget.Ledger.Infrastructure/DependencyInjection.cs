using FreeBudget.Ledger.Application.Interfaces;
using FreeBudget.Ledger.Infrastructure.Persistence;
using FreeBudget.Ledger.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBudget.Ledger.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLedgerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<LedgerDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("LedgerDb")));

        services.AddScoped<ILedgerEntryRepository, LedgerEntryRepository>();

        return services;
    }
}
