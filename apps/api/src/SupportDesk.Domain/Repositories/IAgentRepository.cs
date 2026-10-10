using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Domain.Repositories;

public record AgentWorkload(Agent Agent, int OpenTicketCount);

public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(int id, CancellationToken ct);

    // Fetch active agents and count their open tickets
    Task<IReadOnlyList<AgentWorkload>> GetActiveWorkloadsAsync(CancellationToken ct);
}
