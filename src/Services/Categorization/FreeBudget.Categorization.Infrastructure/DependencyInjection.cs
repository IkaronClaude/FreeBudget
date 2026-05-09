using FreeBudget.Categorization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBudget.Categorization.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCategorizationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CategorizationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("CategorizationDb")));

        return services;
    }
}
