using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Infrastructure.Categorization;
using FreeBudget.Transactions.Infrastructure.Parsing;
using FreeBudget.Transactions.Infrastructure.Persistence;
using FreeBudget.Transactions.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBudget.Transactions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<TransactionsDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("TransactionsDb"))
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IImportBatchRepository, ImportBatchRepository>();
        services.AddScoped<ICategorizationRuleRepository, CategorizationRuleRepository>();
        services.AddScoped<IImportLayoutRepository, ImportLayoutRepository>();
        services.AddScoped<ISharingRuleRepository, SharingRuleRepository>();
        services.AddSingleton<ICsvTransactionParser, CsvTransactionParser>();
        services.AddSingleton<ICategorizer, RuleCategorizer>();

        return services;
    }
}
