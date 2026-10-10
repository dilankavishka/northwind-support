using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Services;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalationTicket;

public class EscalateTicketCommandHandler(
    ITicketRepository tickets,
    ICustomerRepository customers,
    ICategoryRepository categories,
    IAgentRepository agents,
    TicketTriageService triageService,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<EscalateTicketCommandHandler> logger)
{
    public async Task HandleAsync(EscalateTicketCommand request, CancellationToken ct)
    {
        var ticket = await tickets.GetByIdAsync(request.TicketId, ct);
        if (ticket == null) throw new NotFoundException("Ticket not found", request.TicketId);

        var customer = await customers.GetByIdAsync(ticket.CustomerId, ct);
        var category = await categories.GetByIdAsync(ticket.CategoryId, ct);
        var now = clock.UtcNow;

        // Escalation raises priority one level. Must  find the next priority to calculate the new Due Date.
        var nextPriority = ticket.Priority switch
        {
            TicketPriority.Low => TicketPriority.Medium,
            TicketPriority.Medium => TicketPriority.High,
            TicketPriority.High => TicketPriority.Critical,
            _ => ticket.Priority,
        };

        var newDueDate = triageService.CalculateDueDate(nextPriority, customer!, now);

        // Re-evaluate owner. Keep the current agent if still eligible, else reassign.
        var workloads = await agents.GetActiveWorkloadsAsync(ct);

        int? nextAgentId = ticket.AssignedAgentId;
        var currentAgent = workloads.FirstOrDefault(w => w.Agent.Id == nextAgentId);

        bool isCurrentEligible = currentAgent != null && currentAgent.OpenTicketCount < currentAgent.Agent.MaxOpenTickets;
        if (isCurrentEligible && category!.RequiresSpecialist)
        {
            isCurrentEligible = currentAgent!.Agent.Specializations.Any(s => s.CategoryId == category.Id);
        }
        if (!isCurrentEligible)
        {
            var (newAgentId, _) = triageService.Assign(category!, workloads);
            nextAgentId = newAgentId;
        }

        // Apply it to the domain model
        ticket.Escalate(newDueDate, nextAgentId, request.Reason, now);

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Ticket {Reference} escalated to {Priority}. Agent: {AgentId}",
            ticket.Reference, ticket.Priority, nextAgentId);

    }
}
