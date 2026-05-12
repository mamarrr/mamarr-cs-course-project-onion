using App.DAL.EF.Mappers.Admin;
using App.Domain;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit.DAL.Mappers;

public class AdminTicketMonitorDalMapper_Tests
{
    private readonly AdminTicketMonitorDalMapper _mapper = new();

    [Fact]
    public void MapListItem_MapsTicketAndNavigationDisplayValues()
    {
        var createdAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var dueAt = DateTime.UtcNow.AddDays(2);
        var ticket = new Ticket
        {
            Id = new Guid("10000000-0000-0000-0000-000000000001"),
            TicketNr = "T-001",
            Title = TestLangStr.Create("Leaking pipe", "Lekkiv toru"),
            ManagementCompany = new ManagementCompany { Name = "Company A" },
            Customer = new Customer { Name = "Customer A" },
            TicketStatus = new TicketStatus { Code = "CREATED", Label = TestLangStr.Create("Created", "Loodud") },
            TicketPriority = new TicketPriority { Label = TestLangStr.Create("Medium", "Keskmine") },
            TicketCategory = new TicketCategory { Label = TestLangStr.Create("Plumbing", "Torutood") },
            Vendor = new Vendor { Name = "Vendor A" },
            CreatedAt = createdAt,
            DueAt = dueAt
        };

        var dto = _mapper.MapListItem(ticket);

        dto.Id.Should().Be(ticket.Id);
        dto.TicketNumber.Should().Be("T-001");
        dto.Title.Should().Be("Leaking pipe");
        dto.CompanyName.Should().Be("Company A");
        dto.CustomerName.Should().Be("Customer A");
        dto.StatusCode.Should().Be("CREATED");
        dto.StatusLabel.Should().Be("Created");
        dto.PriorityLabel.Should().Be("Medium");
        dto.CategoryLabel.Should().Be("Plumbing");
        dto.VendorName.Should().Be("Vendor A");
        dto.CreatedAt.Should().Be(createdAt);
        dto.DueAt.Should().Be(dueAt);
        dto.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void MapListItem_MissingOptionalNavigations_MapToNullOrEmpty()
    {
        var ticket = new Ticket
        {
            Id = new Guid("10000000-0000-0000-0000-000000000002"),
            TicketNr = "T-002",
            Title = TestLangStr.Create("General issue", "Uldprobleem"),
            CreatedAt = DateTime.UtcNow
        };

        var dto = _mapper.MapListItem(ticket);

        dto.CompanyName.Should().BeEmpty();
        dto.CustomerName.Should().BeNull();
        dto.StatusCode.Should().BeEmpty();
        dto.StatusLabel.Should().BeEmpty();
        dto.PriorityLabel.Should().BeEmpty();
        dto.CategoryLabel.Should().BeEmpty();
        dto.VendorName.Should().BeNull();
    }

    [Fact]
    public void MapListItem_OpenPastDueTicket_IsOverdue()
    {
        var ticket = new Ticket
        {
            Id = new Guid("10000000-0000-0000-0000-000000000003"),
            TicketNr = "T-003",
            Title = TestLangStr.Create("Overdue issue", "Hilinenud probleem"),
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            DueAt = DateTime.UtcNow.AddDays(-1)
        };

        var dto = _mapper.MapListItem(ticket);

        dto.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void MapListItem_ClosedPastDueTicket_IsNotOverdue()
    {
        var ticket = new Ticket
        {
            Id = new Guid("10000000-0000-0000-0000-000000000004"),
            TicketNr = "T-004",
            Title = TestLangStr.Create("Closed issue", "Suletud probleem"),
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            DueAt = DateTime.UtcNow.AddDays(-1),
            ClosedAt = DateTime.UtcNow
        };

        var dto = _mapper.MapListItem(ticket);

        dto.IsOverdue.Should().BeFalse();
    }
}
