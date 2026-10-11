using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
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
    GetTicketQueryHandler getTicket,
    ILogger<EscalateTicketCommandHandler> logger)
{
    public async Task<EscalateTicketResponse> HandleAsync(EscalateTicketCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length is < 5 or > 500)
        {
            throw new ValidationException("Reason", "Reason must be between 5 and 500 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.EscalatedBy) || request.EscalatedBy.Trim().Length > 100)
        {
            throw new ValidationException("EscalatedBy", "EscalatedBy must not be empty and cannot exceed 100 characters.");
        }

        var ticket = await tickets.GetByIdAsync(request.TicketId, ct)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        var customer = await customers.GetByIdAsync(ticket.CustomerId, ct);
        var category = await categories.GetByIdAsync(ticket.CategoryId, ct);
        var now = clock.UtcNow;

        // Next priority for recalculating due date
        var nextPriority = ticket.Priority switch
        {
            TicketPriority.Low => TicketPriority.Medium,
            TicketPriority.Medium => TicketPriority.High,
            TicketPriority.High => TicketPriority.Critical,
            _ => ticket.Priority,
        };

        var newDueDate = triageService.CalculateDueDate(nextPriority, customer!, now);

        // Re-evaluate owner: keep current agent if still eligible, else reassign
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

        // Apply to domain aggregate
        ticket.Escalate(newDueDate, nextAgentId, request.Reason, request.EscalatedBy, now);

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Ticket {Reference} escalated to {Priority}. Agent: {AgentId}",
            ticket.Reference, ticket.Priority, nextAgentId);

        var updatedTicket = await getTicket.HandleAsync(ticket.Id, ct);
        var domainEscalation = ticket.Escalations.Last();

        var fromAgentObj = workloads.FirstOrDefault(w => w.Agent.Id == domainEscalation.FromAgentId)?.Agent;
        var toAgentObj = workloads.FirstOrDefault(w => w.Agent.Id == domainEscalation.ToAgentId)?.Agent;

        var escalationDto = new TicketEscalationDto(
            domainEscalation.Id,
            ticket.Id,
            domainEscalation.FromPriority,
            domainEscalation.ToPriority,
            fromAgentObj == null ? null : new AgentSummaryDto(fromAgentObj.Id, fromAgentObj.FullName),
            toAgentObj == null ? null : new AgentSummaryDto(toAgentObj.Id, toAgentObj.FullName),
            domainEscalation.FromDueAtUtc,
            domainEscalation.ToDueAtUtc,
            domainEscalation.Reason,
            domainEscalation.EscalatedBy,
            domainEscalation.EscalatedAtUtc);

        return new EscalateTicketResponse(updatedTicket, escalationDto);
    }
}
