namespace FreeBudget.Identity.Application.DTOs;

public sealed record UserDto(Guid Id, string Email, string DisplayName);
