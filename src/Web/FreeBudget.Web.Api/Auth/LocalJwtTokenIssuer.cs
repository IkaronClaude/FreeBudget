using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FreeBudget.Web.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FreeBudget.Web.Api.Auth;

internal sealed class LocalJwtTokenIssuer(IOptions<AuthOptions> options) : ITokenIssuer
{
    private readonly AuthOptions _options = options.Value;

    public IssuedToken Issue(UserDto user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.JwtLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.JwtIssuer,
            audience: _options.JwtAudience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return new IssuedToken(encoded, expires);
    }
}
