using FreeBudget.Web.Api.Clients;
using FreeBudget.Web.Api.Models;
using Microsoft.Extensions.Options;

namespace FreeBudget.Web.Api.CurrentUser;

public sealed class CurrentUserOptions
{
    public string Email { get; set; } = "admin@freebudget.local";
}

public sealed class SeededAdminCurrentUserResolver(
    IdentityClient identity,
    IOptions<CurrentUserOptions> options,
    ILogger<SeededAdminCurrentUserResolver> logger) : ICurrentUserResolver
{
    private UserDto? _cached;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<UserDto> GetAsync(CancellationToken ct)
    {
        if (_cached is not null) return _cached;

        await _gate.WaitAsync(ct);
        try
        {
            if (_cached is not null) return _cached;

            var email = options.Value.Email;
            var user = await identity.GetUserByEmailAsync(email, ct)
                ?? throw new InvalidOperationException(
                    $"Seeded user '{email}' not found. Ensure the Identity service is running and seeded.");

            logger.LogInformation("Resolved current user {Email} = {UserId}", email, user.Id);
            _cached = user;
            return user;
        }
        finally
        {
            _gate.Release();
        }
    }
}
