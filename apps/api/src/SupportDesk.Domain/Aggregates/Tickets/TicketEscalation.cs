namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Immutable history record of an escalated ticket
/// </summary>
public sealed class TicketEscalation
{
    private TicketEscalation() {}

    public TicketEscalation(string reason, DateTime escalatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length is < 5 or > 500)
        {
            throw new ArgumentException("Reason must be between 5 and 500 characters.", nameof(reason));
        }

        Reason = reason;
        EscalatedAtUtc = escalatedAtUtc;
    }

    public int Id { get; private set; }
    public string Reason { get; private set; }
    public DateTime EscalatedAtUtc { get; private set; }
}
