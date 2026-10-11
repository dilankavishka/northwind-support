using SupportDesk.Infrastructure.Services;
using Xunit;

namespace SupportDesk.UnitTests.Infrastructure.Services;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void HashPassword_ProducesSaltedHash_DifferentEachTimeForSamePassword()
    {
        var password = "SecurePassword123!";

        var hash1 = _sut.HashPassword(password);
        var hash2 = _sut.HashPassword(password);

        Assert.NotEmpty(hash1);
        Assert.NotEmpty(hash2);
        Assert.NotEqual(hash1, hash2); // Salts make hashes distinct
    }

    [Fact]
    public void VerifyPassword_WhenPasswordMatches_ReturnsTrue()
    {
        var password = "Admin123";
        var hash = _sut.HashPassword(password);

        var result = _sut.VerifyPassword(password, hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WhenPasswordDoesNotMatch_ReturnsFalse()
    {
        var hash = _sut.HashPassword("CorrectPassword");

        var result = _sut.VerifyPassword("WrongPassword", hash);

        Assert.False(result);
    }

    [Theory]
    [InlineData("", "somehash")]
    [InlineData("password", "")]
    [InlineData("password", "malformed")]
    public void VerifyPassword_WhenInputIsInvalid_ReturnsFalse(string password, string hash)
    {
        var result = _sut.VerifyPassword(password, hash);

        Assert.False(result);
    }
}
