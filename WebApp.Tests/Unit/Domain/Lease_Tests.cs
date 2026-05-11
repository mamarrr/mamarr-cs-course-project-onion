using App.Domain;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit.Domain;

public class Lease_Tests
{
    [Fact]
    public void Lease_ConnectsResidentUnitAndRole()
    {
        var residentId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 12, 31);

        var lease = new Lease
        {
            ResidentId = residentId,
            UnitId = unitId,
            LeaseRoleId = roleId,
            StartDate = startDate,
            EndDate = endDate,
            Notes = TestLangStr.Create("Primary lease", "Peamine leping")
        };

        lease.ResidentId.Should().Be(residentId);
        lease.UnitId.Should().Be(unitId);
        lease.LeaseRoleId.Should().Be(roleId);
        lease.StartDate.Should().Be(startDate);
        lease.EndDate.Should().Be(endDate);
        lease.Notes!.Translate("en").Should().Be("Primary lease");
        lease.Notes.Translate("et").Should().Be("Peamine leping");
    }

    [Fact]
    public void Lease_EndDate_IsOptional()
    {
        var lease = new Lease
        {
            StartDate = new DateOnly(2026, 1, 1)
        };

        lease.EndDate.Should().BeNull();
    }
}
