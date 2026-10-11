namespace SupportDesk.Application.Configuration;

/// <summary>
/// Configuration for JWT authentication token issuance and validation.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Cryptographic signing secret. Configured in appsettings or JWT_SECRET_KEY environment variable.
    /// Default development fallback provided.
    /// </summary>
    public string SecretKey { get; set; } = "SupportDeskDevSuperSecretSigningKeyForJwtTokens123456!";

    public string Issuer { get; set; } = "SupportDesk";

    public string Audience { get; set; } = "SupportDesk";

    /// <summary>Token lifetime in hours. Standard 8-hour shift.</summary>
    public int ExpiryHours { get; set; } = 8;
}
