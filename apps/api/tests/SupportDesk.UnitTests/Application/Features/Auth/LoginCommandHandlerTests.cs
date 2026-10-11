using Moq;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Auth.Commands.Login;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.UnitTests.Application.Features.Auth;

public sealed class LoginCommandHandlerTests
{
    private readonly Mock<IAgentRepository> _agentRepoMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenService> _tokenServiceMock = new();

    private LoginCommandHandler CreateHandler() =>
        new(_agentRepoMock.Object, _passwordHasherMock.Object, _tokenServiceMock.Object);

    [Fact]
    public async Task HandleAsync_ValidActiveAgent_ReturnsTokenAndAgentDto()
    {
        var agent = new Agent("Alex Turner", "alex@northwind.com", 5, DateTime.UtcNow);
        agent.SetPasswordHash("valid_hashed_password");

        _agentRepoMock
            .Setup(r => r.GetByEmailAsync("alex@northwind.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("Admin123", "valid_hashed_password"))
            .Returns(true);

        var expiresAt = DateTime.UtcNow.AddHours(8);
        _tokenServiceMock
            .Setup(s => s.GenerateToken(agent))
            .Returns(("test.jwt.token", expiresAt));

        var handler = CreateHandler();
        var response = await handler.HandleAsync(
            new LoginRequest("alex@northwind.com", "Admin123"),
            CancellationToken.None);

        Assert.Equal("test.jwt.token", response.Token);
        Assert.Equal(expiresAt, response.ExpiresAtUtc);
        Assert.Equal("Alex Turner", response.Agent.FullName);
        Assert.Equal("alex@northwind.com", response.Agent.Email);
    }

    [Theory]
    [InlineData("", "Admin123")]
    [InlineData("   ", "Admin123")]
    [InlineData("alex@northwind.com", "")]
    [InlineData("alex@northwind.com", "   ")]
    public async Task HandleAsync_EmptyCredentials_ThrowsAuthenticationFailedException(string email, string password)
    {
        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.HandleAsync(new LoginRequest(email, password), CancellationToken.None));

        Assert.Equal("Invalid email or password.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_NonExistentEmail_ThrowsIdentical401Message()
    {
        _agentRepoMock
            .Setup(r => r.GetByEmailAsync("unknown@northwind.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Agent?)null);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.HandleAsync(new LoginRequest("unknown@northwind.com", "Admin123"), CancellationToken.None));

        Assert.Equal("Invalid email or password.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_DeactivatedAgent_ThrowsIdentical401Message()
    {
        var agent = new Agent("Yuki Tanaka", "yuki@northwind.com", 5, DateTime.UtcNow);
        agent.Deactivate();
        agent.SetPasswordHash("valid_hashed_password");

        _agentRepoMock
            .Setup(r => r.GetByEmailAsync("yuki@northwind.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.HandleAsync(new LoginRequest("yuki@northwind.com", "Admin123"), CancellationToken.None));

        Assert.Equal("Invalid email or password.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_ThrowsIdentical401Message()
    {
        var agent = new Agent("Alex Turner", "alex@northwind.com", 5, DateTime.UtcNow);
        agent.SetPasswordHash("valid_hashed_password");

        _agentRepoMock
            .Setup(r => r.GetByEmailAsync("alex@northwind.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("WrongPassword", "valid_hashed_password"))
            .Returns(false);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.HandleAsync(new LoginRequest("alex@northwind.com", "WrongPassword"), CancellationToken.None));

        Assert.Equal("Invalid email or password.", ex.Message);
    }
}
