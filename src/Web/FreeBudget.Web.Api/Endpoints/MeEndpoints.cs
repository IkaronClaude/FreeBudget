using FreeBudget.Web.Api.Clients;
using FreeBudget.Web.Api.CurrentUser;
using FreeBudget.Web.Api.Models;

namespace FreeBudget.Web.Api.Endpoints;

public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", async (
            ICurrentUserResolver currentUser,
            IdentityClient identity,
            CancellationToken ct) =>
        {
            var user = await currentUser.GetAsync(ct);
            var groupsTask = identity.GetUserGroupsAsync(user.Id, ct);
            var accountsTask = identity.GetUserBankAccountsAsync(user.Id, ct);
            await Task.WhenAll(groupsTask, accountsTask);

            return Results.Ok(new MeResponse(user, await groupsTask, await accountsTask));
        });

        return app;
    }
}
