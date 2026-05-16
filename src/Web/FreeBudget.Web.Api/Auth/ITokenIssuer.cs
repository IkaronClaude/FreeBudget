using FreeBudget.Web.Api.Models;

namespace FreeBudget.Web.Api.Auth;

public sealed record IssuedToken(string AccessToken, DateTime ExpiresAt);

public interface ITokenIssuer
{
    IssuedToken Issue(UserDto user);
}
