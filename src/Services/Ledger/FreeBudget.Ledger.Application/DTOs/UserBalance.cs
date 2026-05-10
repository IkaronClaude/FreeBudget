using FreeBudget.SharedKernel.ValueObjects;

namespace FreeBudget.Ledger.Application.DTOs;

public sealed record UserBalance(
    Guid UserId,
    Guid OwesToUserId,
    Money NetAmount);
