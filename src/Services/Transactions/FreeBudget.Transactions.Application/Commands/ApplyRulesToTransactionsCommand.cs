using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Application.Interfaces;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

public sealed record ApplyRulesToTransactionsCommand(
    Guid UserId,
    IReadOnlyList<Guid> BankAccountIds) : IRequest<Result<ApplyRulesResult>>;

public sealed record ApplyRulesResult(int Examined, int Updated);

internal sealed class ApplyRulesToTransactionsHandler(
    ICategorizationRuleRepository ruleRepository,
    ITransactionRepository transactionRepository,
    ICategorizer categorizer)
    : IRequestHandler<ApplyRulesToTransactionsCommand, Result<ApplyRulesResult>>
{
    public async Task<Result<ApplyRulesResult>> Handle(
        ApplyRulesToTransactionsCommand request,
        CancellationToken cancellationToken)
    {
        var rules = await ruleRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (rules.Count == 0)
            return Result<ApplyRulesResult>.Success(new ApplyRulesResult(0, 0));

        var examined = 0;
        var updated = 0;

        foreach (var bankAccountId in request.BankAccountIds)
        {
            var transactions = await transactionRepository
                .GetByBankAccountIdAsync(bankAccountId, cancellationToken);

            foreach (var txn in transactions.Where(t => t.Category is null))
            {
                examined++;
                var category = categorizer.Categorize(txn.Description, rules);
                if (category is null) continue;

                txn.UpdateCategory(category);
                await transactionRepository.UpdateAsync(txn, cancellationToken);
                updated++;
            }
        }

        return Result<ApplyRulesResult>.Success(new ApplyRulesResult(examined, updated));
    }
}
