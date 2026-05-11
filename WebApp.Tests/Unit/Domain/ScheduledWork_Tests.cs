using App.Domain;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit.Domain;

public class ScheduledWork_Tests
{
    [Fact]
    public void ScheduledWork_StoresScheduledRangeAndRequiredRelationships()
    {
        var scheduledStart = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var scheduledEnd = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var vendorId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var statusId = Guid.NewGuid();

        var work = new ScheduledWork
        {
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledEnd,
            VendorId = vendorId,
            TicketId = ticketId,
            WorkStatusId = statusId,
            Notes = TestLangStr.Create("Bring parts", "Vota varuosad kaasa")
        };

        work.ScheduledStart.Should().Be(scheduledStart);
        work.ScheduledEnd.Should().Be(scheduledEnd);
        work.VendorId.Should().Be(vendorId);
        work.TicketId.Should().Be(ticketId);
        work.WorkStatusId.Should().Be(statusId);
        work.Notes!.Translate("en").Should().Be("Bring parts");
        work.Notes.Translate("et").Should().Be("Vota varuosad kaasa");
    }

    [Fact]
    public void ScheduledWork_RealStartAndEnd_AreInitiallyNull()
    {
        var work = new ScheduledWork();

        work.RealStart.Should().BeNull();
        work.RealEnd.Should().BeNull();
    }
}
