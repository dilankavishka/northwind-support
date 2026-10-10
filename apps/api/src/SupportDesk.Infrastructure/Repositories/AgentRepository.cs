using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Infrastructure.Repositories;

public sealed class AgentRepository(SupportDbContext db) : IAgentRepository
{
    /// <summary>Loads the whole aggregate, specializations included.</summary>
    public Task<Agent?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Set<Agent>()
            .Include(a => a.Specializations)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<AgentWorkload>> GetActiveWorkloadsAsync(CancellationToken ct)
    {
        var workloads = await db.Set<Agent>()
            .Include(a => a.Specializations)
            .Where(a => a.IsActive)
            .Select(a => new
            {
                Agent = a,
                OpenCount = db.Set<Ticket>().Count(t =>
                    t.AssignedAgentId == a.Id &&
                    t.Status != TicketStatus.Resolved &&
                    t.Status != TicketStatus.Closed)
            })
            .ToListAsync(ct);
            
        return workloads.Select(w => new AgentWorkload(w.Agent, w.OpenCount)).ToList();
    }
}
