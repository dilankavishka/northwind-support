namespace SupportDesk.Application.Contracts.Auth;

/// <summary>Request payload for POST /api/auth/login.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Authenticated agent profile returned on login and /api/auth/me.</summary>
public sealed record CurrentAgentDto(int Id, string FullName, string Email);

/// <summary>Response payload containing JWT and agent metadata.</summary>
public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAtUtc,
    CurrentAgentDto Agent);
