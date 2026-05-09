using Microsoft.Extensions.DependencyInjection;

namespace FreeBudget.Categorization.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCategorizationApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
