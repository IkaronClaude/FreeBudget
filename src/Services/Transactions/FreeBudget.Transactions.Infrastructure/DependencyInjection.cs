using FreeBudget.Transactions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBudget.Transactions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<TransactionsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("TransactionsDb")));

        return services;
    }
}
