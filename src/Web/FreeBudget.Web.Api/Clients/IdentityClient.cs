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
