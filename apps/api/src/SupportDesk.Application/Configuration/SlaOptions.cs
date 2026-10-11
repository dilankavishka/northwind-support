using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Configuration;

public class SlaOptions
{
    public const String SectionName = "Sla";

    public Dictionary<TicketPriority, int> BaseWindowHours { get; set; } = new()
    {
        [TicketPriority.Critical] = 4,
        [TicketPriority.High] = 8,
        [TicketPriority.Medium] = 24,
        [TicketPriority.Low] = 72
    };

    public double PremiumCustomerMultiplier { get; set; } = 0.5;

    public int MinimumWindowHours { get; set; } = 1;

    public int AtRiskThresholdPercentage { get; set; } = 25;
}
