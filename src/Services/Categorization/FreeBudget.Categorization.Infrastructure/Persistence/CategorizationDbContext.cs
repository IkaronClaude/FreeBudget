using FreeBudget.Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Categorization.Infrastructure.Persistence;

public sealed class CategorizationDbContext(DbContextOptions<CategorizationDbContext> options)
    : BaseDbContext(options)
{
}
