using FreeBudget.Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Transactions.Infrastructure.Persistence;

public sealed class TransactionsDbContext(DbContextOptions<TransactionsDbContext> options)
    : BaseDbContext(options)
{
}
