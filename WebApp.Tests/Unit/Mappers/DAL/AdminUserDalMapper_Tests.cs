using App.DAL.EF.Mappers.Admin;
using App.Domain;
using App.Domain.Identity;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit.Mappers.DAL;

public class AdminUserDalMapper_Tests
{
    private readonly AdminUserDalMapper _mapper = new();

    [Fact]
    public void MapUser_MapsScalarsAndAdminFlag()
    {
        var createdAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var lockoutEnd = DateTimeOffset.UtcNow.AddDays(1);
        var user = new AppUser
        {
            Id = new Guid("10000000-0000-0000-0000-000000000001"),
            Email = "admin@test.ee",
            FirstName = "System",
            LastName = "Admin",
            CreatedAt = createdAt,
            LockoutEnd = lockoutEnd
        };

        var dto = _mapper.Map(user, hasSystemAdminRole: true);

        dto.Id.Should().Be(user.Id);
        dto.Email.Should().Be(user.Email);
        dto.FullName.Should().Be("System Admin");
        dto.CreatedAt.Should().Be(createdAt);
        dto.LockoutEnd.Should().Be(lockoutEnd.UtcDateTime);
        dto.IsLocked.Should().BeTrue();
        dto.HasSystemAdminRole.Should().BeTrue();
    }

    [Fact]
    public void MapUser_NullEmailMapsToEmptyString_AndNameIsTrimmed()
    {
        var user = new AppUser
        {
            Id = new Guid("10000000-0000-0000-0000-000000000002"),
            Email = null,
            FirstName = "Single",
            LastName = "",
            CreatedAt = DateTime.UtcNow
        };

        var dto = _mapper.Map(user, hasSystemAdminRole: false);

        dto.Email.Should().BeEmpty();
        dto.FullName.Should().Be("Single");
        dto.IsLocked.Should().BeFalse();
        dto.HasSystemAdminRole.Should().BeFalse();
    }

    [Fact]
    public void MapMembership_MapsNavigationValues_WhenPresent()
    {
        var validFrom = new DateOnly(2026, 1, 1);
        var validTo = new DateOnly(2026, 12, 31);
        var membership = new ManagementCompanyUser
        {
            Id = new Guid("20000000-0000-0000-0000-000000000001"),
            ManagementCompanyId = new Guid("30000000-0000-0000-0000-000000000001"),
            ManagementCompany = new ManagementCompany { Name = "Company A" },
            ManagementCompanyRole = new ManagementCompanyRole
            {
                Code = "OWNER",
                Label = TestLangStr.Create("Owner", "Omanik")
            },
            ValidFrom = validFrom,
            ValidTo = validTo
        };

        var dto = _mapper.Map(membership);

        dto.MembershipId.Should().Be(membership.Id);
        dto.CompanyId.Should().Be(membership.ManagementCompanyId);
        dto.CompanyName.Should().Be("Company A");
        dto.RoleCode.Should().Be("OWNER");
        dto.RoleLabel.Should().Be("Owner");
        dto.ValidFrom.Should().Be(validFrom);
        dto.ValidTo.Should().Be(validTo);
    }

    [Fact]
    public void MapMembership_MissingNavigations_MapToEmptyStrings()
    {
        var membership = new ManagementCompanyUser
        {
            Id = new Guid("20000000-0000-0000-0000-000000000002"),
            ManagementCompanyId = new Guid("30000000-0000-0000-0000-000000000002")
        };

        var dto = _mapper.Map(membership);

        dto.CompanyName.Should().BeEmpty();
        dto.RoleCode.Should().BeEmpty();
        dto.RoleLabel.Should().BeEmpty();
    }
}
