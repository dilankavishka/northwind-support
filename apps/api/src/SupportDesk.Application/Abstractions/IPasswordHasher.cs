namespace SupportDesk.Application.Abstractions;

/// <summary>
/// Hashes and verifies passwords using a cryptographically secure, salted mechanism.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Produces a salted hash representation of the password.</summary>
    string HashPassword(string password);
    /// <summary>Checks whether a plaintext password matches the stored salted hash.</summary>
    bool VerifyPassword(string password, string passwordHash);
}
