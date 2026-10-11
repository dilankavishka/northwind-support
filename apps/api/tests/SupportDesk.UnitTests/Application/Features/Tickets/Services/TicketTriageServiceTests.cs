using Microsoft.Extensions.Options;
using SupportDesk.Application.Configuration;
using SupportDesk.Application.Features.Tickets.Services;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Services;

public sealed class TicketTriageServiceTests
{
    private readonly TicketTriageService _service;
    private readonly DateTime _now = FixedClock.DefaultNow;

    public TicketTriageServiceTests()
    {
        var options = Options.Create(new SlaOptions());
        _service = new TicketTriageService(options);
    }

    [Fact]
    public void DeterminePriority_DefaultsToMedium_WhenNullRequested()
    {
        var category = new Category("General", false, false);
        var priority = _service.DeterminePriority(null, category);

        Assert.Equal(TicketPriority.Medium, priority);
    }

    [Fact]
    public void DeterminePriority_ForcesCritical_WhenCategoryFlagsIt()
    {
        var securityCategory = new Category("Security", requiresSpecialist: true, forcesCriticalPriority: true);
        var priority = _service.DeterminePriority(TicketPriority.Low, securityCategory);

        Assert.Equal(TicketPriority.Critical, priority);
    }

    [Theory]
    [InlineData(TicketPriority.Critical, 4)]
    [InlineData(TicketPriority.High, 8)]
    [InlineData(TicketPriority.Medium, 24)]
    [InlineData(TicketPriority.Low, 72)]
    public void CalculateDueDate_StandardCustomer_GetsBaseWindow(TicketPriority priority, int expectedHours)
    {
        var customer = new Customer("Standard Co", "std@test.com", null, CustomerTier.Standard, _now);
        var due = _service.CalculateDueDate(priority, customer, _now);

        Assert.Equal(_now.AddHours(expectedHours), due);
    }

    [Theory]
    [InlineData(TicketPriority.Critical, 2)]
    [InlineData(TicketPriority.High, 4)]
    [InlineData(TicketPriority.Medium, 12)]
    [InlineData(TicketPriority.Low, 36)]
    public void CalculateDueDate_PremiumCustomer_GetsHalfWindow(TicketPriority priority, int expectedHours)
    {
        var customer = new Customer("Premium Co", "prem@test.com", null, CustomerTier.Premium, _now);
        var due = _service.CalculateDueDate(priority, customer, _now);

        Assert.Equal(_now.AddHours(expectedHours), due);
    }

    [Fact]
    public void CalculateDueDate_RespectsOneHourFloor()
    {
        // Custom options with 1h base and 0.5x multiplier -> 30 mins, but floor is 1h
        var customOptions = Options.Create(new SlaOptions
        {
            BaseWindowHours = new() { [TicketPriority.Critical] = 1 },
            PremiumCustomerMultiplier = 0.5,
            MinimumWindowHours = 1
        });
        var service = new TicketTriageService(customOptions);

        var premiumCustomer = new Customer("Prem", "p@p.com", null, CustomerTier.Premium, _now);
        var due = service.CalculateDueDate(TicketPriority.Critical, premiumCustomer, _now);

        Assert.Equal(_now.AddHours(1), due);
    }

    [Fact]
    public void Assign_SelectsAgentWithFewestOpenTickets()
    {
        var category = new Category("General", false, false);
        var agent1 = CreateAgent(1, "Agent 1");
        var agent2 = CreateAgent(2, "Agent 2");

        var workloads = new List<AgentWorkload>
        {
            new(agent1, 5),
            new(agent2, 2)
        };

        var (agentId, reason) = _service.Assign(category, workloads);

        Assert.Equal(2, agentId);
        Assert.Contains("Agent 2", reason);
    }

    [Fact]
    public void Assign_DeterministicTieBreakByAgentId()
    {
        var category = new Category("General", false, false);
        var agent10 = CreateAgent(10, "Agent 10");
        var agent5 = CreateAgent(5, "Agent 5");

        var workloads = new List<AgentWorkload>
        {
            new(agent10, 3),
            new(agent5, 3)
        };

        var (agentId, _) = _service.Assign(category, workloads);

        // Lower agent id (5) wins the tie-break
        Assert.Equal(5, agentId);
    }

    [Fact]
    public void Assign_RequiresSpecialist_OnlySelectsSpecialist()
    {
        var category = new Category("Billing", requiresSpecialist: true, false).WithId(99);

        var generalist = CreateAgent(1, "Generalist");
        var specialist = CreateAgent(2, "Specialist");
        specialist.AddSpecialization(99);

        var workloads = new List<AgentWorkload>
        {
            new(generalist, 0),
            new(specialist, 3)
        };

        var (agentId, _) = _service.Assign(category, workloads);

        Assert.Equal(2, agentId);
    }

    [Fact]
    public void Assign_ReturnsNull_WhenAllAgentsAtMaxCapacity()
    {
        var category = new Category("General", false, false);
        var agent = CreateAgent(1, "Busy Agent", maxOpen: 5);

        var workloads = new List<AgentWorkload>
        {
            new(agent, 5) // At capacity
        };

        var (agentId, reason) = _service.Assign(category, workloads);

        Assert.Null(agentId);
        Assert.Contains("No agent has capacity", reason);
    }

    private static Agent CreateAgent(int id, string name, int maxOpen = 10)
    {
        var agent = new Agent(name, $"{name.ToLowerInvariant().Replace(" ", "")}@test.com", maxOpen, FixedClock.DefaultNow);
        return agent.WithId(id);
    }
}
