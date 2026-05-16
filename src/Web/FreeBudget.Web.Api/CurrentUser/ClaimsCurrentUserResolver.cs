using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FreeBudget.Web.Api.Models;

namespace FreeBudget.Web.Api.CurrentUser;

public sealed class ClaimsCurrentUserResolver(IHttpContextAccessor accessor) : ICurrentUserResolver
{
    public Task<UserDto> GetAsync(CancellationToken ct)
    {
        var principal = accessor.HttpContext?.User
            ?? throw new UnauthorizedAccessException("No HttpContext available.");

        if (principal.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("Request is not authenticated.");

        var idClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idClaim, out var userId))
            throw new UnauthorizedAccessException("JWT does not carry a valid user id.");

        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? principal.FindFirstValue(ClaimTypes.Email)
            ?? "";
        var name = principal.FindFirstValue(JwtRegisteredClaimNames.Name)
            ?? principal.FindFirstValue(ClaimTypes.Name)
            ?? "";

        return Task.FromResult(new UserDto(userId, email, name));
    }
}
