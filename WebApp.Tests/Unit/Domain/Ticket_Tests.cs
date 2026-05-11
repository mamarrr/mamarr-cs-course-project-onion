using App.Domain;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit.Domain;

public class Ticket_Tests
{
    [Fact]
    public void Ticket_CanBeCreatedWithRequiredCoreFields()
    {
        var ticket = new Ticket
        {
            ManagementCompanyId = new Guid("10000000-0000-0000-0000-000000000001"),
            TicketNr = "T-001",
            Title = TestLangStr.Create("Leaking pipe", "Lekkiv toru"),
            Description = TestLangStr.Create("Water leak in bathroom", "Veeleke vannitoas"),
            TicketCategoryId = new Guid("20000000-0000-0000-0000-000000000001"),
            TicketStatusId = new Guid("30000000-0000-0000-0000-000000000001"),
            TicketPriorityId = new Guid("40000000-0000-0000-0000-000000000001"),
            CreatedAt = DateTime.UtcNow
        };

        ticket.Id.Should().NotBe(Guid.Empty);
        ticket.ManagementCompanyId.Should().NotBe(Guid.Empty);
        ticket.TicketNr.Should().Be("T-001");
        ticket.TicketCategoryId.Should().NotBe(Guid.Empty);
        ticket.TicketStatusId.Should().NotBe(Guid.Empty);
        ticket.TicketPriorityId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Ticket_AllowsOptionalContextLinks()
    {
        var customerId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var residentId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();

        var ticket = new Ticket
        {
            CustomerId = customerId,
            PropertyId = propertyId,
            UnitId = unitId,
            ResidentId = residentId,
            VendorId = vendorId
        };

        ticket.CustomerId.Should().Be(customerId);
        ticket.PropertyId.Should().Be(propertyId);
        ticket.UnitId.Should().Be(unitId);
        ticket.ResidentId.Should().Be(residentId);
        ticket.VendorId.Should().Be(vendorId);
    }

    [Fact]
    public void Ticket_TitleAndDescription_PreserveMultipleLanguages()
    {
        var ticket = new Ticket
        {
            Title = TestLangStr.Create("Leaking pipe", "Lekkiv toru"),
            Description = TestLangStr.Create("Water leak in bathroom", "Veeleke vannitoas")
        };

        ticket.Title.Translate("en").Should().Be("Leaking pipe");
        ticket.Title.Translate("et").Should().Be("Lekkiv toru");
        ticket.Description.Translate("en").Should().Be("Water leak in bathroom");
        ticket.Description.Translate("et").Should().Be("Veeleke vannitoas");
    }

    [Fact]
    public void Ticket_DateFields_DefaultToNullUntilWorkflowSetsThem()
    {
        var ticket = new Ticket();

        ticket.DueAt.Should().BeNull();
        ticket.ClosedAt.Should().BeNull();
    }
}
