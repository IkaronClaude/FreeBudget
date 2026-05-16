using System.Net;
using FreeBudget.Web.Api.Models;

namespace FreeBudget.Web.Api.Clients;

public sealed record IdentityResult<T>(bool IsSuccess, T? Value, string? Error)
{
    public static IdentityResult<T> Ok(T value) => new(true, value, null);
    public static IdentityResult<T> Fail(string error) => new(false, default, error);
}

public sealed class IdentityClient(HttpClient http)
{
    public async Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken ct)
    {
        var response = await http.GetAsync($"/api/users?email={Uri.EscapeDataString(email)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct);
    }

    public async Task<UserDto?> VerifyCredentialsAsync(string email, string password, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync(
            "/api/auth/verify", new { Email = email, Password = password }, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct);
    }

    public async Task<IdentityResult<UserDto>> RegisterAsync(
        string email, string displayName, string password, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync(
            "/api/auth/register",
            new { Email = email, DisplayName = displayName, Password = password },
            ct);

        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            var err = await response.Content.ReadFromJsonAsync<ErrorPayload>(cancellationToken: ct);
            return IdentityResult<UserDto>.Fail(err?.Error ?? "Registration failed.");
        }

        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct);
        return IdentityResult<UserDto>.Ok(user!);
    }

    private sealed record ErrorPayload(string Error);

    public async Task<IReadOnlyList<GroupDto>> GetUserGroupsAsync(Guid userId, CancellationToken ct)
    {
        var response = await http.GetAsync($"/api/users/{userId}/groups", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<GroupDto>>(cancellationToken: ct) ?? [];
    }

    public async Task<IReadOnlyList<BankAccountDto>> GetUserBankAccountsAsync(Guid userId, CancellationToken ct)
    {
        var response = await http.GetAsync($"/api/users/{userId}/bank-accounts", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<BankAccountDto>>(cancellationToken: ct) ?? [];
    }

    public HttpClient Http => http;
}

public static class BankAccountHierarchyExtensions
{
    // Layout, group access, and other metadata live on the parent for child accounts.
    public static Guid ResolveLayoutOwnerId(this IReadOnlyList<BankAccountDto> accounts, Guid bankAccountId)
    {
        var account = accounts.FirstOrDefault(a => a.Id == bankAccountId);
        return account?.ParentBankAccountId ?? bankAccountId;
    }

    public static IReadOnlyDictionary<string, Guid> BuildCurrencyAccountMap(
        this IReadOnlyList<BankAccountDto> accounts, Guid parentId)
    {
        var map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var child in accounts.Where(a => a.ParentBankAccountId == parentId && !string.IsNullOrWhiteSpace(a.CurrencyCode)))
        {
            map[child.CurrencyCode!.ToUpperInvariant()] = child.Id;
        }
        return map;
    }
}
