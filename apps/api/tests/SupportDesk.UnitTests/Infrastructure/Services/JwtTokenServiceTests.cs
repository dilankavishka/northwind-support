using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Moq;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Configuration;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Infrastructure.Services;

namespace SupportDesk.UnitTests.Infrastructure.Services;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void GenerateToken_ProducesValidJwt_WithExpectedClaimsAndExpiry()
    {
        var fixedNow = new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);
        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.UtcNow).Returns(fixedNow);

        var options = Options.Create(new JwtOptions
        {
            SecretKey = "SuperSecretKeyForTestingJwtTokenServiceMustBeLongEnough123!",
            Issuer = "SupportDeskTestIssuer",
            Audience = "SupportDeskTestAudience",
            ExpiryHours = 8
        });

        var service = new JwtTokenService(options, clockMock.Object);
        var agent = new Agent("Alex Turner", "alex@northwind.com", 5, fixedNow);

        var (token, expiresAtUtc) = service.GenerateToken(agent);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Equal(fixedNow.AddHours(8), expiresAtUtc);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("SupportDeskTestIssuer", jwt.Issuer);
        Assert.Contains("SupportDeskTestAudience", jwt.Audiences);
        Assert.Equal(agent.Id.ToString(), jwt.Subject);
        Assert.Equal("Alex Turner", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name || c.Type == ClaimTypes.Name).Value);
        Assert.Equal("alex@northwind.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email || c.Type == ClaimTypes.Email).Value);
    }
}
