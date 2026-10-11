using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Auth.Queries.GetCurrentAgent;

/// <summary>
/// Retrieves authenticated agent details for /api/auth/me.
/// </summary>
public sealed class GetCurrentAgentQueryHandler(IAgentRepository agentRepository)
{
    public async Task<CurrentAgentDto> HandleAsync(int agentId, CancellationToken ct)
    {
        var agent = await agentRepository.GetByIdAsync(agentId, ct);
        if (agent == null || !agent.IsActive)
        {
            throw new AuthenticationFailedException("Agent not found or inactive.");
        }

        return new CurrentAgentDto(agent.Id, agent.FullName, agent.Email);
    }
}
