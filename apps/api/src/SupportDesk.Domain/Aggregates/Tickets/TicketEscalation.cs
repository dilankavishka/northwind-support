namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Immutable history record of an escalated ticket
/// </summary>
public sealed class TicketEscalation
{
    private TicketEscalation() {}

    public TicketEscalation(
        int ticketId,
        TicketPriority fromPriority,
        TicketPriority toPriority,
        int? fromAgentId,
        int? toAgentId,
        DateTime? fromDueAtUtc,
        DateTime? toDueAtUtc,
        string reason,
        string escalatedBy,
        DateTime escalatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length is < 5 or > 500)
        {
            throw new ArgumentException("Reason must be between 5 and 500 characters.", nameof(reason));
        }

        if (string.IsNullOrWhiteSpace(escalatedBy) || escalatedBy.Trim().Length > 100)
        {
            throw new ArgumentException("EscalatedBy must not be empty and cannot exceed 100 characters.",
                nameof(escalatedBy));
        }

        TicketId = ticketId;
        FromPriority = fromPriority;
        ToPriority = toPriority;
        FromAgentId = fromAgentId;
        ToAgentId = toAgentId;
        FromDueAtUtc = fromDueAtUtc;
        ToDueAtUtc = toDueAtUtc;
        Reason = reason;
        EscalatedBy = escalatedBy;
        EscalatedAtUtc = escalatedAtUtc;
    }

    public int Id { get; private set; }

    public int TicketId { get; private set; }

    public TicketPriority FromPriority { get; private set; }

    public TicketPriority ToPriority { get; private set; }

    public int? FromAgentId { get; private set; }

    public int? ToAgentId { get; private set; }

    public DateTime? FromDueAtUtc { get; private set; }

    public DateTime? ToDueAtUtc { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public string EscalatedBy { get; private set; } = string.Empty;

    public DateTime EscalatedAtUtc { get; private set; }
}
