using App.BLL.DTO.Admin.Users;
using App.BLL.Mappers.Admin;
using App.DAL.DTO.Admin.Users;
using AwesomeAssertions;

namespace WebApp.Tests.Unit.BLL.Mappers;

public class AdminUserBllMapper_Tests
{
    private readonly AdminUserBllMapper _mapper = new();

    [Fact]
    public void MapSearch_MapsAllFilters()
    {
        var createdFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var createdTo = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var dto = new AdminUserSearchDto
        {
            SearchText = "admin",
            Email = "admin@test.ee",
            Name = "Admin",
            LockedOnly = true,
            HasSystemAdminRole = true,
            CreatedFrom = createdFrom,
            CreatedTo = createdTo
        };

        var dal = _mapper.Map(dto);

        dal.SearchText.Should().Be(dto.SearchText);
        dal.Email.Should().Be(dto.Email);
        dal.Name.Should().Be(dto.Name);
        dal.LockedOnly.Should().BeTrue();
        dal.HasSystemAdminRole.Should().BeTrue();
        dal.CreatedFrom.Should().Be(createdFrom);
        dal.CreatedTo.Should().Be(createdTo);
    }

    [Fact]
    public void MapListItem_MapsAllScalars()
    {
        var createdAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var lockoutEnd = new DateTime(2027, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        var dto = _mapper.Map(new AdminUserListItemDalDto
        {
            Id = new Guid("10000000-0000-0000-0000-000000000001"),
            Email = "admin@test.ee",
            FullName = "System Admin",
            CreatedAt = createdAt,
            LockoutEnd = lockoutEnd,
            IsLocked = true,
            HasSystemAdminRole = true
        });

        dto.Id.Should().Be(new Guid("10000000-0000-0000-0000-000000000001"));
        dto.Email.Should().Be("admin@test.ee");
        dto.FullName.Should().Be("System Admin");
        dto.CreatedAt.Should().Be(createdAt);
        dto.LockoutEnd.Should().Be(lockoutEnd);
        dto.IsLocked.Should().BeTrue();
        dto.HasSystemAdminRole.Should().BeTrue();
    }

    [Fact]
    public void MapDetails_MapsRolesAndMemberships()
    {
        var roleId = new Guid("20000000-0000-0000-0000-000000000001");
        var membershipId = new Guid("30000000-0000-0000-0000-000000000001");
        var companyId = new Guid("40000000-0000-0000-0000-000000000001");
        var validFrom = new DateOnly(2026, 1, 1);
        var validTo = new DateOnly(2026, 12, 31);

        var dto = _mapper.Map(new AdminUserDetailsDalDto
        {
            Id = new Guid("10000000-0000-0000-0000-000000000001"),
            Email = "admin@test.ee",
            FullName = "System Admin",
            PhoneNumber = "+372",
            LastLoginAt = DateTime.UtcNow,
            RefreshTokenCount = 2,
            Roles = [new AdminUserRoleDalDto { RoleId = roleId, RoleName = "SystemAdmin" }],
            CompanyMemberships =
            [
                new AdminUserCompanyMembershipDalDto
                {
                    MembershipId = membershipId,
                    CompanyId = companyId,
                    CompanyName = "Company A",
                    RoleCode = "OWNER",
                    RoleLabel = "Owner",
                    ValidFrom = validFrom,
                    ValidTo = validTo
                }
            ]
        });

        dto.PhoneNumber.Should().Be("+372");
        dto.RefreshTokenCount.Should().Be(2);
        dto.Roles.Should().ContainSingle();
        dto.Roles[0].RoleId.Should().Be(roleId);
        dto.Roles[0].RoleName.Should().Be("SystemAdmin");
        dto.CompanyMemberships.Should().ContainSingle();
        dto.CompanyMemberships[0].MembershipId.Should().Be(membershipId);
        dto.CompanyMemberships[0].CompanyId.Should().Be(companyId);
        dto.CompanyMemberships[0].CompanyName.Should().Be("Company A");
        dto.CompanyMemberships[0].RoleCode.Should().Be("OWNER");
        dto.CompanyMemberships[0].RoleLabel.Should().Be("Owner");
        dto.CompanyMemberships[0].ValidFrom.Should().Be(validFrom);
        dto.CompanyMemberships[0].ValidTo.Should().Be(validTo);
    }
}
