namespace FreeBudget.Web.Api.Auth;

public sealed class AuthOptions
{
    public string JwtIssuer { get; set; } = "freebudget-web-api";
    public string JwtAudience { get; set; } = "freebudget-web-ui";
    public string JwtSigningKey { get; set; } = "";
    public int JwtLifetimeMinutes { get; set; } = 60 * 12;
}
