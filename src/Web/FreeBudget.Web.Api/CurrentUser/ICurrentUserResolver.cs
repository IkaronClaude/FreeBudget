using FreeBudget.Web.Api.Models;

namespace FreeBudget.Web.Api.CurrentUser;

public interface ICurrentUserResolver
{
    Task<UserDto> GetAsync(CancellationToken ct);
}
