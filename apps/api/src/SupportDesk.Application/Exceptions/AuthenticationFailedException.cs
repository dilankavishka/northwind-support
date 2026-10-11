namespace SupportDesk.Application.Exceptions;

/// <summary>
/// Thrown when authentication credentials cannot be verified or agent is inactive.
/// Returns HTTP 401 Unauthorized.
/// </summary>
public sealed class AuthenticationFailedException(string message = "Invalid email or password.")
    : Exception(message);
