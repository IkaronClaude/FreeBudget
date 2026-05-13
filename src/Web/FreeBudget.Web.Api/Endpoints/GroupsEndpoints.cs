using FreeBudget.Web.Api.Clients;
using FreeBudget.Web.Api.CurrentUser;
using FreeBudget.Web.Api.Models;

namespace FreeBudget.Web.Api.Endpoints;

public static class GroupsEndpoints
{
    public static IEndpointRouteBuilder MapGroupsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/groups", async (
            CreateGroupDto body,
            ICurrentUserResolver currentUser,
            IdentityClient identity,
            CancellationToken ct) =>
        {
            var me = await currentUser.GetAsync(ct);
            var payload = new { body.Name, CreatedByUserId = me.Id, body.CreatorLabel };
            var response = await identity.Http.PostAsJsonAsync("/api/groups", payload, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        app.MapPut("/api/groups/{id:guid}", async (
            Guid id,
            RenameGroupDto body,
            IdentityClient identity,
            CancellationToken ct) =>
        {
            var response = await identity.Http.PutAsJsonAsync($"/api/groups/{id}", body, ct);
            return response.IsSuccessStatusCode
                ? Results.NoContent()
                : Results.StatusCode((int)response.StatusCode);
        });

        app.MapDelete("/api/groups/{id:guid}", async (
            Guid id,
            IdentityClient identity,
            CancellationToken ct) =>
        {
            var response = await identity.Http.DeleteAsync($"/api/groups/{id}", ct);
            return response.IsSuccessStatusCode
                ? Results.NoContent()
                : Results.StatusCode((int)response.StatusCode);
        });

        app.MapPost("/api/groups/{id:guid}/members", async (
            Guid id,
            AddGroupMemberDto body,
            IdentityClient identity,
            CancellationToken ct) =>
        {
            var response = await identity.Http.PostAsJsonAsync($"/api/groups/{id}/members", body, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        app.MapPut("/api/groups/{groupId:guid}/members/{memberId:guid}", async (
            Guid groupId,
            Guid memberId,
            RenameGroupMemberDto body,
            IdentityClient identity,
            CancellationToken ct) =>
        {
            var response = await identity.Http.PutAsJsonAsync($"/api/groups/{groupId}/members/{memberId}", body, ct);
            return response.IsSuccessStatusCode
                ? Results.NoContent()
                : Results.StatusCode((int)response.StatusCode);
        });

        app.MapDelete("/api/groups/{groupId:guid}/members/{memberId:guid}", async (
            Guid groupId,
            Guid memberId,
            IdentityClient identity,
            CancellationToken ct) =>
        {
            var response = await identity.Http.DeleteAsync($"/api/groups/{groupId}/members/{memberId}", ct);
            return response.IsSuccessStatusCode
                ? Results.NoContent()
                : Results.StatusCode((int)response.StatusCode);
        });

        app.MapPost("/api/groups/{groupId:guid}/members/{memberId:guid}/link", async (
            Guid groupId,
            Guid memberId,
            LinkGroupMemberDto body,
            IdentityClient identity,
            CancellationToken ct) =>
        {
            var response = await identity.Http.PostAsJsonAsync(
                $"/api/groups/{groupId}/members/{memberId}/link", body, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        return app;
    }
}
