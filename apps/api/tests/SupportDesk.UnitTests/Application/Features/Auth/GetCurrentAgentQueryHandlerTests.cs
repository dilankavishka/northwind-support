using Moq;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Auth.Queries.GetCurrentAgent;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.UnitTests.Application.Features.Auth;

public sealed class GetCurrentAgentQueryHandlerTests
{
    private readonly Mock<IAgentRepository> _agentRepoMock = new();

    [Fact]
    public async Task HandleAsync_ActiveAgent_ReturnsCurrentAgentDto()
    {
        var agent = new Agent("Alex Turner", "alex@northwind.com", 5, DateTime.UtcNow);
        _agentRepoMock
            .Setup(r => r.GetByIdAsync(agent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        var handler = new GetCurrentAgentQueryHandler(_agentRepoMock.Object);
        var result = await handler.HandleAsync(agent.Id, CancellationToken.None);

        Assert.Equal(agent.Id, result.Id);
        Assert.Equal("Alex Turner", result.FullName);
        Assert.Equal("alex@northwind.com", result.Email);
    }

    [Fact]
    public async Task HandleAsync_AgentNotFound_ThrowsAuthenticationFailedException()
    {
        _agentRepoMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Agent?)null);

        var handler = new GetCurrentAgentQueryHandler(_agentRepoMock.Object);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.HandleAsync(999, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_InactiveAgent_ThrowsAuthenticationFailedException()
    {
        var agent = new Agent("Yuki Tanaka", "yuki@northwind.com", 5, DateTime.UtcNow);
        agent.Deactivate();
        _agentRepoMock
            .Setup(r => r.GetByIdAsync(agent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        var handler = new GetCurrentAgentQueryHandler(_agentRepoMock.Object);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => handler.HandleAsync(agent.Id, CancellationToken.None));
    }
}
