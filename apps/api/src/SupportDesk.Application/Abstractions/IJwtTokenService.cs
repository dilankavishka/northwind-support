using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Application.Abstractions;

/// <summary>
/// Issues signed JSON Web Tokens for authenticated agents.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT with subject, name, and email claims.
    /// </summary>
    (string Token, DateTime ExpiresAtUtc) GenerateToken(Agent agent);
}
