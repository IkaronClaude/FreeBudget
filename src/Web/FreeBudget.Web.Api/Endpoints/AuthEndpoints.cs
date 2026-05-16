using FreeBudget.Web.Api.Auth;
using FreeBudget.Web.Api.Clients;

namespace FreeBudget.Web.Api.Endpoints;

public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterRequest(string Email, string DisplayName, string Password);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, Models.UserDto User);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            IdentityClient identity,
            ITokenIssuer issuer,
            CancellationToken ct) =>
        {
            var user = await identity.VerifyCredentialsAsync(request.Email, request.Password, ct);
            if (user is null)
                return Results.Unauthorized();

            var token = issuer.Issue(user);
            return Results.Ok(new AuthResponse(token.AccessToken, token.ExpiresAt, user));
        }).AllowAnonymous();

        app.MapPost("/api/auth/register", async (
            RegisterRequest request,
            IdentityClient identity,
            ITokenIssuer issuer,
            CancellationToken ct) =>
        {
            var result = await identity.RegisterAsync(request.Email, request.DisplayName, request.Password, ct);
            if (!result.IsSuccess)
                return Results.UnprocessableEntity(new { result.Error });

            var token = issuer.Issue(result.Value!);
            return Results.Ok(new AuthResponse(token.AccessToken, token.ExpiresAt, result.Value!));
        }).AllowAnonymous();

        return app;
    }
}
