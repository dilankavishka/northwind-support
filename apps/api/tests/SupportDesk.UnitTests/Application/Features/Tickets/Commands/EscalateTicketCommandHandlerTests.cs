using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Commands.EscalationTicket;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Exceptions;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Commands;

public sealed class EscalateTicketCommandHandlerTests
{
    private readonly TicketCommandTestContext _ctx = new();

    [Fact]
    public async Task HandleAsync_RaisesPriorityByOneLevel_AndRecordsEscalation()
    {
        var ticket = new TicketBuilder()
            .WithPriority(TicketPriority.Medium)
            .Build()
            .WithId(10);

        _ctx.ExistingTicket(ticket);

        var command = new EscalateTicketCommand(ticket.Id, "Escalating because issue is impacting customer billing.", "lead@northwind.com");
        var response = await _ctx.EscalateTicket.HandleAsync(command, CancellationToken.None);

        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.Single(ticket.Escalations);

        var escalation = ticket.Escalations.Single();
        Assert.Equal(TicketPriority.Medium, escalation.FromPriority);
        Assert.Equal(TicketPriority.High, escalation.ToPriority);
        Assert.Equal("lead@northwind.com", escalation.EscalatedBy);
        Assert.Equal("Escalating because issue is impacting customer billing.", escalation.Reason);

        _ctx.VerifySaved(Times.Once());
    }

    [Fact]
    public async Task HandleAsync_WhenTicketIsCritical_ThrowsBusinessRuleViolationException()
    {
        var ticket = new TicketBuilder()
            .WithPriority(TicketPriority.Critical)
            .Build()
            .WithId(10);

        _ctx.ExistingTicket(ticket);

        var command = new EscalateTicketCommand(ticket.Id, "Already at critical priority.", "lead@northwind.com");

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _ctx.EscalateTicket.HandleAsync(command, CancellationToken.None));

        Assert.Contains("Critical", ex.Message);
        _ctx.VerifySaved(Times.Never());
    }

    [Fact]
    public async Task HandleAsync_WhenTicketIsResolved_ThrowsBusinessRuleViolationException()
    {
        var ticket = new TicketBuilder()
            .WithStatus(TicketStatus.Resolved)
            .Build()
            .WithId(10);

        _ctx.ExistingTicket(ticket);

        var command = new EscalateTicketCommand(ticket.Id, "Customer says not solved.", "lead@northwind.com");

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _ctx.EscalateTicket.HandleAsync(command, CancellationToken.None));

        Assert.Contains("resolved", ex.Message);
        _ctx.VerifySaved(Times.Never());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Tiny")] // 4 characters
    public async Task HandleAsync_WhenReasonIsTooShort_ThrowsValidationException(string reason)
    {
        var ticket = new TicketBuilder().Build().WithId(10);
        _ctx.ExistingTicket(ticket);

        var command = new EscalateTicketCommand(ticket.Id, reason, "lead@northwind.com");

        await Assert.ThrowsAsync<ValidationException>(
            () => _ctx.EscalateTicket.HandleAsync(command, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WhenEscalatedByIsMissing_ThrowsValidationException(string actor)
    {
        var ticket = new TicketBuilder().Build().WithId(10);
        _ctx.ExistingTicket(ticket);

        var command = new EscalateTicketCommand(ticket.Id, "Valid reason here", actor);

        await Assert.ThrowsAsync<ValidationException>(
            () => _ctx.EscalateTicket.HandleAsync(command, CancellationToken.None));
    }
}
