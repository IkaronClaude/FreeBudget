using FreeBudget.Web.Api.Clients;

namespace FreeBudget.Web.Api.Endpoints;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/lookup", async (
            string email,
            IdentityClient identity,
            CancellationToken ct) =>
        {
            var user = await identity.GetUserByEmailAsync(email, ct);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        return app;
    }
}
