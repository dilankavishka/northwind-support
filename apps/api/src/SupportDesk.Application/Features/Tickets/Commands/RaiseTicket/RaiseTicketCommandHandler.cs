using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Services;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Commands.RaiseTicket;

/// <summary>
/// Raises a ticket for a customer.
/// </summary>
public sealed class RaiseTicketCommandHandler(
    ITicketRepository tickets,
    ICustomerRepository customers,
    ICategoryRepository categories,
    IAgentRepository agents,
    TicketTriageService triageService,
    IUnitOfWork unitOfWork,
    IClock clock,
    GetTicketQueryHandler getTicket,
    ILogger<RaiseTicketCommandHandler> logger)
{
    public async Task<TicketDetailDto> HandleAsync(CreateTicketRequest request, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(request.CustomerId, ct);
        if (customer == null) throw new NotFoundException("Customer not found", request.CustomerId);

        var category = await categories.GetByIdAsync(request.CategoryId, ct);
        if (category == null) throw new NotFoundException("Category not found", request.CategoryId);

        var now = clock.UtcNow;
        var priority = triageService.DeterminePriority(request.RequestedPriority, category);
        var dueDate = triageService.CalculateDueDate(priority, customer, now);

        // Raise the ticket with the calculated priority
        var ticket = Ticket.Raise(
            await tickets.NextReferenceAsync(ct),
            request.Title,
            request.Description,
            request.CustomerId,
            request.CategoryId,
            priority,
            now);

        // Will assign ticket for the best agent
        var workloads = await agents.GetActiveWorkloadsAsync(ct);
        var (agentId, reason) = triageService.Assign(category, workloads);
        ticket.AssignTo(agentId, now);

        // Save and return
        await tickets.AddAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation(
            "Ticket {Reference} raised. Priority: {Priority}, Agent: {AgentId}, Reason: {Reason}",
            ticket.Reference, priority, agentId, reason);
        return await getTicket.HandleAsync(ticket.Id, ct);
    }
}
