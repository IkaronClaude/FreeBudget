using FreeBudget.Web.Api.Models;

namespace FreeBudget.Web.Api.Clients;

public sealed class TransactionsClient(HttpClient http)
{
    public HttpClient Http => http;

    public async Task<IReadOnlyList<TransactionListItem>> ListAsync(Guid bankAccountId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var url = $"/api/transactions?bankAccountId={bankAccountId}";
        if (from.HasValue) url += $"&from={Uri.EscapeDataString(from.Value.ToString("o"))}";
        if (to.HasValue) url += $"&to={Uri.EscapeDataString(to.Value.ToString("o"))}";
        var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<TransactionListItem>>(cancellationToken: ct) ?? [];
    }

    public async Task<IReadOnlyList<CategoryBreakdownItem>> CategoryBreakdownAsync(Guid bankAccountId, DateTime from, DateTime to, bool excludeTransfers, CancellationToken ct)
    {
        var url = $"/api/reports/category-breakdown?bankAccountId={bankAccountId}&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}&excludeTransfers={excludeTransfers}";
        var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CategoryBreakdownItem>>(cancellationToken: ct) ?? [];
    }

    public async Task<IReadOnlyList<PeriodBreakdownItem>> PeriodBreakdownAsync(Guid bankAccountId, DateTime from, DateTime to, string granularity, bool excludeTransfers, CancellationToken ct)
    {
        var url = $"/api/reports/period-breakdown?bankAccountId={bankAccountId}&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}&granularity={Uri.EscapeDataString(granularity)}&excludeTransfers={excludeTransfers}";
        var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<PeriodBreakdownItem>>(cancellationToken: ct) ?? [];
    }
}
