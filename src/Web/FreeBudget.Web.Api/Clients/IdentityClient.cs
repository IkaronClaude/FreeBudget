using System.Net;
using FreeBudget.Web.Api.Models;

namespace FreeBudget.Web.Api.Clients;

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
