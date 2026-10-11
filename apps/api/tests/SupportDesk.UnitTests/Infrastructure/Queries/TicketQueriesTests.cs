using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Queries;

public sealed class TicketQueriesTests : IDisposable
{
    private readonly SupportDbContext _db;
    private readonly TicketQueries _queries;
    private readonly FixedClock _clock = new();

    public TicketQueriesTests()
    {
        // In-memory database for testing the queries
        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlite("DataSource=:memory:").Options;

        _db = new SupportDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        _queries = new TicketQueries(_db, _clock);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetPagedAsync_FiltersByExactMatchPriority()
    {
        await SeedDataAsync();
        var query = new TicketQuery { Priority = TicketPriority.Critical };
        var result = await _queries.GetPagedAsync(query, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Contains(result.Items, t => t.Priority == TicketPriority.Critical);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByCombinationOfFilters()
    {
        await SeedDataAsync();
        var query = new TicketQuery { Priority = TicketPriority.High, Status = TicketStatus.InProgress };
        var result = await _queries.GetPagedAsync(query, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TCK-003", result.Items.Single().Reference);
    }

    [Fact]
    public async Task GetPagedAsync_SearchMatchesCustomerName()
    {
        await SeedDataAsync();
        var query = new TicketQuery { Search = " Batman" };
        var result = await _queries.GetPagedAsync(query, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersBySlaStatus_Met()
    {
        await SeedDataAsync();
        var query = new TicketQuery { SlaStatus = SlaStatus.Met };
        var result = await _queries.GetPagedAsync(query, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TCK-004", result.Items.Single().Reference);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersBySlaStatus_Breached()
    {
        await SeedDataAsync();
        var query = new TicketQuery { SlaStatus = SlaStatus.Breached };
        var result = await _queries.GetPagedAsync(query, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TCK-005", result.Items.Single().Reference);
    }

    private async Task SeedDataAsync()
    {
        var now = _clock.UtcNow;
        var customer1 = new Customer("Batman", "b@b.com", null, CustomerTier.Premium, now);
        var customer2 = new Customer("Spiderman", "s@s.com", null, CustomerTier.Standard, now);
        var category = new Category("General", false, false);

        _db.Set<Customer>().AddRange(customer1, customer2);
        _db.Set<Category>().Add(category);
        await _db.SaveChangesAsync();

        var ticket1 = Ticket.Raise("TCK-001", "Help", "Body", customer1.Id, category.Id,
            TicketPriority.Critical, now);
        var ticket2 = Ticket.Raise("TCK-002", "Help2", "Body", customer2.Id, category.Id,
            TicketPriority.High, now);
        var ticket3 = Ticket.Raise("TCK-003", "Help3", "Body", customer2.Id, category.Id,
            TicketPriority.High, now);
        var ticket4 = Ticket.Raise("TCK-004", "Help4", "Body", customer2.Id, category.Id,
            TicketPriority.Medium, now.AddHours(-5), dueAtUtc: now.AddHours(2));
        var ticket5 = Ticket.Raise("TCK-005", "Help5", "Body", customer2.Id, category.Id,
            TicketPriority.Medium, now.AddHours(-10), dueAtUtc: now.AddHours(-2));

        ticket3.ChangeStatus(TicketStatus.InProgress, now);
        ticket4.ChangeStatus(TicketStatus.Resolved, now.AddHours(-1));

        _db.Set<Ticket>().AddRange(ticket1, ticket2, ticket3, ticket4, ticket5);
        await _db.SaveChangesAsync();
    }
}
