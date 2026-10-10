namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Works out where a ticket stands against its due date. SLA status is always derived from
/// the ticket's dates and the current time - never stored.
/// </summary>
public static class SlaEvaluator
{
    public static SlaStatus Evaluate(DateTime createdAtUtc, DateTime? dueAtUtc, DateTime? resolvedAtUtc,
        DateTime nowUtc)
    {
        if (dueAtUtc is null)
            return SlaStatus.NotApplicable;

        if (resolvedAtUtc is not null)
            return resolvedAtUtc <= dueAtUtc ? SlaStatus.Met : SlaStatus.Breached;

        if (nowUtc > dueAtUtc)
            return SlaStatus.Breached;

        // Calculate the total time window allowed for this ticket
        var totalWindow = dueAtUtc.Value - createdAtUtc;

        // Calculate how much time is remaining right now
        var timeRemaining = dueAtUtc.Value - nowUtc;

        // If the remaining time is 25% or less of the total window, it is AtRisk
        var percentRemaining = timeRemaining.TotalMilliseconds / totalWindow.TotalMilliseconds;

        if (percentRemaining <= 0.25)
            return SlaStatus.AtRisk;
        return SlaStatus.WithinSla;
    }
}
