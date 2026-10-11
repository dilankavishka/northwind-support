using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Configuration;
using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Infrastructure.Services;

/// <summary>
/// Issues JWT tokens with HMAC-SHA256 signature and standard identity claims.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options, IClock clock) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTime ExpiresAtUtc) GenerateToken(Agent agent)
    {
        var expiresAtUtc = clock.UtcNow.AddHours(_options.ExpiryHours);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, agent.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, agent.FullName),
            new Claim(JwtRegisteredClaimNames.Email, agent.Email),
            new Claim(ClaimTypes.NameIdentifier, agent.Id.ToString()),
            new Claim(ClaimTypes.Name, agent.FullName),
            new Claim(ClaimTypes.Email, agent.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAtUtc,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = credentials,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return (tokenHandler.WriteToken(token), expiresAtUtc);
    }
}
