using FreeBudget.Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Ledger.Infrastructure.Persistence;

public sealed class LedgerDbContext(DbContextOptions<LedgerDbContext> options)
    : BaseDbContext(options)
{
}
