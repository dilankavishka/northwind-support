namespace SupportDesk.Application.Features.Tickets.Commands.EscalationTicket;

public record EscalateTicketCommand(int TicketId, string Reason);
