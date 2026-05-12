using FreeBudget.SharedKernel.ValueObjects;

namespace FreeBudget.Ledger.Application.DTOs;

public sealed record MemberBalance(
    Guid MemberId,
    Guid OwesToMemberId,
    Money NetAmount);
