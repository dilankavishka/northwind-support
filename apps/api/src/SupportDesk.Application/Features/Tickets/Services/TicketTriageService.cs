using Microsoft.Extensions.Options;
using SupportDesk.Application.Configuration;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Services;

public class TicketTriageService(IOptions<SlaOptions> options)
{
    private readonly SlaOptions _sla = options.Value;

    public TicketPriority DeterminePriority(TicketPriority? requestedPriority, Category category)
    {
        // BR-1: Use the requested priority; default medium
        if (category.ForcesCriticalPriority)
        {
            return TicketPriority.Critical;
        }

        return requestedPriority ?? TicketPriority.Medium;
    }

    public DateTime CalculateDueDate(TicketPriority priority, Customer customer, DateTime createdAtUtc)
    {
        // BR-2: Due date equals to creation time + base window
        // BR-3: Premium customers get half the window, never shorter than one hour
        var baseHours = _sla.BaseWindowHours.GetValueOrDefault(priority, 24);
        var calculatedHours = (double)baseHours;

        if (customer.Tier == CustomerTier.Premium)
        {
            calculatedHours *= _sla.PremiumCustomerMultiplier;
        }

        var finalHours = Math.Max(calculatedHours, _sla.MinimumWindowHours);

        return createdAtUtc.AddHours(finalHours);
    }

    public (int? AgentId, string Reason) Assign(Category category, IReadOnlyList<AgentWorkload> workloads)
    {
        var eligible = workloads.AsEnumerable();
        // BR-4: Specialists
        if (category.RequiresSpecialist)
        {
            eligible = eligible.Where(w => w.Agent.Specializations.Any(s => s.CategoryId == category.Id));
        }

        // BR-5: Strictly below MaxOpenTickets
        eligible = eligible.Where(w => w.OpenTicketCount < w.Agent.MaxOpenTickets);

        // BR-5: Fewest open tickets. Deterministic tie-break using Agent Id
        var best = eligible
            .OrderBy(w => w.OpenTicketCount)
            .ThenBy(w => w.Agent.Id)
            .FirstOrDefault();
        if (best != null)
        {
            return (best.Agent.Id, $"Assigned to {best.Agent.FullName} ({best.OpenTicketCount} open).");
        }

        // BR-5: If nobody is eligible, create the ticket unassigned
        var reason = category.RequiresSpecialist
            ? "No specialist agent has capacity."
            : "No agent has capacity.";

        return (null, reason);
    }

    public (TicketPriority Priority, string Reason) DeterminePriorityWithReason(TicketPriority? requestedPriority, Category category)
    {
        if (category.ForcesCriticalPriority)
        {
            return (TicketPriority.Critical, $"Category '{category.Name}' forces Critical priority.");
        }

        var priority = requestedPriority ?? TicketPriority.Medium;
        var reason = requestedPriority.HasValue
            ? $"Applied requested priority '{priority}'."
            : "Defaulted to Medium priority (none requested).";

        return (priority, reason);
    }

    public (DateTime DueDate, string Reason) CalculateDueDateWithReason(TicketPriority priority, Customer customer, DateTime createdAtUtc)
    {
        var baseHours = _sla.BaseWindowHours.GetValueOrDefault(priority, 24);
        var calculatedHours = (double)baseHours;
        string tierDetail = customer.Tier == CustomerTier.Premium
            ? $", halved for Premium customer ({calculatedHours * _sla.PremiumCustomerMultiplier:0.#}h)"
            : "";

        if (customer.Tier == CustomerTier.Premium)
        {
            calculatedHours *= _sla.PremiumCustomerMultiplier;
        }

        var finalHours = Math.Max(calculatedHours, _sla.MinimumWindowHours);
        string floorDetail = calculatedHours < _sla.MinimumWindowHours ? $" (enforced {_sla.MinimumWindowHours}h minimum floor)" : "";

        var dueDate = createdAtUtc.AddHours(finalHours);
        var reason = $"{priority} priority base window: {baseHours}h{tierDetail}{floorDetail}. Due in {finalHours:0.#}h.";
        return (dueDate, reason);
    }

}
