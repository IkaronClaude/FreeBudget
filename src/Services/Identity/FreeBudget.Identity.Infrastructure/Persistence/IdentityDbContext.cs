using FreeBudget.Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : BaseDbContext(options)
{
}
