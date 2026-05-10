namespace FreeBudget.Web.Api.Clients;

public sealed class LedgerClient(HttpClient http)
{
    public HttpClient Http => http;
}
