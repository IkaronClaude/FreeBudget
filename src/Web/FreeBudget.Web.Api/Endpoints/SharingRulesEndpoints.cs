using System.Net;
using FreeBudget.Web.Api.Clients;
using FreeBudget.Web.Api.CurrentUser;
using FreeBudget.Web.Api.Models;

namespace FreeBudget.Web.Api.Endpoints;

public static class SharingRulesEndpoints
{
    public static IEndpointRouteBuilder MapSharingRulesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sharing-rules", async (
            ICurrentUserResolver currentUser,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var me = await currentUser.GetAsync(ct);
            var response = await client.Http.GetAsync($"/api/sharing-rules?userId={me.Id}", ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        app.MapPost("/api/sharing-rules", async (
            CreateSharingRuleInputDto body,
            ICurrentUserResolver currentUser,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var me = await currentUser.GetAsync(ct);
            var payload = new
            {
                UserId = me.Id,
                body.Pattern, body.MatchType, body.EntryType, body.Priority,
                body.GroupId, body.PaidByMemberId, body.ParticipantMemberIds,
            };
            var response = await client.Http.PostAsJsonAsync("/api/sharing-rules", payload, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        app.MapPut("/api/sharing-rules/{id:guid}", async (
            Guid id,
            UpdateSharingRuleInputDto body,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var response = await client.Http.PutAsJsonAsync($"/api/sharing-rules/{id}", body, ct);
            return response.IsSuccessStatusCode
                ? Results.NoContent()
                : Results.StatusCode((int)response.StatusCode);
        });

        app.MapDelete("/api/sharing-rules/{id:guid}", async (
            Guid id,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var response = await client.Http.DeleteAsync($"/api/sharing-rules/{id}", ct);
            return response.IsSuccessStatusCode
                ? Results.NoContent()
                : Results.StatusCode((int)response.StatusCode);
        });

        app.MapPost("/api/sharing-rules/apply", async (
            ICurrentUserResolver currentUser,
            IdentityClient identity,
            TransactionsClient transactions,
            LedgerClient ledger,
            CancellationToken ct) =>
        {
            var me = await currentUser.GetAsync(ct);
            var result = await RunApplyAsync(me.Id, identity, transactions, ledger, ct);
            return Results.Ok(result);
        });

        return app;
    }

    public static async Task<ApplySharingRulesResult> RunApplyAsync(
        Guid userId,
        IdentityClient identity,
        TransactionsClient transactions,
        LedgerClient ledger,
        CancellationToken ct)
    {
        var rulesResp = await transactions.Http.GetAsync($"/api/sharing-rules?userId={userId}", ct);
        rulesResp.EnsureSuccessStatusCode();
        var rules = await rulesResp.Content.ReadFromJsonAsync<List<SharingRuleDto>>(cancellationToken: ct) ?? [];
        if (rules.Count == 0)
            return new ApplySharingRulesResult(0, 0, 0, 0, 0, 0);

        var orderedRules = rules.OrderByDescending(r => r.Priority).ToList();
        var accounts = await identity.GetUserBankAccountsAsync(userId, ct);

        var examined = 0;
        var matched = 0;
        var split = 0;
        var skipped = 0;
        var excluded = 0;

        foreach (var account in accounts)
        {
            var txnResp = await transactions.Http.GetAsync($"/api/transactions?bankAccountId={account.Id}", ct);
            if (!txnResp.IsSuccessStatusCode) continue;
            var txns = await txnResp.Content.ReadFromJsonAsync<List<TransactionListItem>>(cancellationToken: ct) ?? [];

            foreach (var txn in txns)
            {
                examined++;
                var rule = orderedRules.FirstOrDefault(r => Matches(r, txn.Description));
                if (rule is null) continue;
                matched++;

                // Exclude rule wins: explicitly don't auto-share this transaction.
                if (string.Equals(rule.EntryType, "Exclude", StringComparison.OrdinalIgnoreCase))
                {
                    excluded++;
                    continue;
                }

                HttpResponseMessage resp;
                if (string.Equals(rule.EntryType, "Settlement", StringComparison.OrdinalIgnoreCase))
                {
                    var settlementPayload = new
                    {
                        rule.GroupId,
                        rule.PaidByMemberId,
                        OwedByMemberId = rule.ParticipantMemberIds[0],
                        Amount = decimal.Round(Math.Abs(txn.Amount), 2),
                        CurrencyCode = txn.CurrencyCode,
                        Description = txn.Description,
                        EntryDate = txn.TransactionDate,
                        CreatedByUserId = userId,
                        TransactionId = (Guid?)txn.Id,
                    };
                    resp = await ledger.Http.PostAsJsonAsync("/api/ledger/settlements", settlementPayload, ct);
                }
                else
                {
                    var perHead = decimal.Round(Math.Abs(txn.Amount) / rule.ParticipantMemberIds.Count, 2);
                    var owers = rule.ParticipantMemberIds
                        .Where(id => id != rule.PaidByMemberId)
                        .Select(id => new SplitParticipantInputDto(id, perHead))
                        .ToList();
                    if (owers.Count == 0) { skipped++; continue; }

                    var splitPayload = new
                    {
                        rule.GroupId,
                        rule.PaidByMemberId,
                        TransactionId = txn.Id,
                        CurrencyCode = txn.CurrencyCode,
                        Description = txn.Description,
                        EntryDate = txn.TransactionDate,
                        CreatedByUserId = userId,
                        Participants = owers,
                    };
                    resp = await ledger.Http.PostAsJsonAsync("/api/ledger/splits", splitPayload, ct);
                }

                if (resp.IsSuccessStatusCode) split++;
                else if (resp.StatusCode == HttpStatusCode.UnprocessableEntity) skipped++;
                else resp.EnsureSuccessStatusCode();
            }
        }

        var transfersPaired = 0;
        if (accounts.Count > 1)
        {
            var matchPayload = new { BankAccountIds = accounts.Select(a => a.Id).ToList(), DateToleranceDays = (int?)1 };
            var matchResp = await transactions.Http.PostAsJsonAsync("/api/transactions/match-transfers", matchPayload, ct);
            if (matchResp.IsSuccessStatusCode)
            {
                var matchResult = await matchResp.Content.ReadFromJsonAsync<MatchTransfersResultDto>(cancellationToken: ct);
                transfersPaired = matchResult?.Matched ?? 0;
            }
        }

        return new ApplySharingRulesResult(examined, matched, split, skipped, excluded, transfersPaired);
    }

    private static bool Matches(SharingRuleDto rule, string description) =>
        rule.MatchType.ToLowerInvariant() switch
        {
            "any" => true,
            "contains" => description.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase),
            "exact" => description.Equals(rule.Pattern, StringComparison.OrdinalIgnoreCase),
            "startswith" => description.StartsWith(rule.Pattern, StringComparison.OrdinalIgnoreCase),
            "endswith" => description.EndsWith(rule.Pattern, StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
}
