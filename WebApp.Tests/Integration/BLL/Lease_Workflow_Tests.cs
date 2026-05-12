using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Leases;
using App.BLL.DTO.Residents;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.BLL;

public class Lease_Workflow_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private const string CustomerASlug = "customer-a";
    private const string PropertyASlug = "property-a";
    private const string UnitASlug = "a-101";

    private readonly CustomWebApplicationFactory _factory;

    public Lease_Workflow_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatesListsUpdatesAndDeletesFromResidentAndUnitRoutes()
    {
        var resident = await CreateResidentAsync("resident-lease");
        var leaseRoleId = await LeaseRoleIdAsync("TENANT");
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        Guid leaseId;
        using (var createScope = _factory.Services.CreateScope())
        {
            var bll = createScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var created = await bll.Leases.CreateForResidentAndGetDetailsAsync(
                ResidentRoute(resident.IdCode),
                new LeaseBllDto
                {
                    UnitId = TestTenants.UnitAId,
                    LeaseRoleId = leaseRoleId,
                    StartDate = start,
                    Notes = "Resident route lease"
                });

            created.IsSuccess.Should().BeTrue();
            created.Value.ResidentId.Should().Be(resident.ResidentId);
            created.Value.UnitId.Should().Be(TestTenants.UnitAId);
            leaseId = created.Value.LeaseId;
        }

        using (var listScope = _factory.Services.CreateScope())
        {
            var bll = listScope.ServiceProvider.GetRequiredService<IAppBLL>();

            var byResident = await bll.Leases.ListForResidentAsync(ResidentRoute(resident.IdCode));
            var byUnit = await bll.Leases.ListForUnitAsync(UnitRoute());
            var detailsByUnit = await bll.Leases.GetForUnitAsync(UnitLeaseRoute(leaseId));

            byResident.Value.Leases.Should().Contain(lease => lease.LeaseId == leaseId);
            byUnit.Value.Leases.Should().Contain(lease => lease.LeaseId == leaseId && lease.ResidentId == resident.ResidentId);
            detailsByUnit.Value.LeaseId.Should().Be(leaseId);
        }

        using (var updateScope = _factory.Services.CreateScope())
        {
            var bll = updateScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var updated = await bll.Leases.UpdateFromUnitAndGetDetailsAsync(
                UnitLeaseRoute(leaseId),
                new LeaseBllDto
                {
                    LeaseRoleId = leaseRoleId,
                    StartDate = start,
                    EndDate = start.AddDays(20),
                    Notes = "Updated from unit route"
                });

            updated.IsSuccess.Should().BeTrue();
            updated.Value.EndDate.Should().Be(start.AddDays(20));
            updated.Value.Notes.Should().Be("Updated from unit route");
        }

        using var deleteScope = _factory.Services.CreateScope();
        var deleteBll = deleteScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var deleted = await deleteBll.Leases.DeleteFromResidentAsync(ResidentLeaseRoute(resident.IdCode, leaseId));
        var afterDelete = await deleteBll.Leases.ListForResidentAsync(ResidentRoute(resident.IdCode));

        deleted.IsSuccess.Should().BeTrue();
        afterDelete.Value.Leases.Should().NotContain(lease => lease.LeaseId == leaseId);
    }

    [Fact]
    public async Task ValidationAndResidentDeleteDependency_AreEnforced()
    {
        var resident = await CreateResidentAsync("resident-lease-invalid");
        var leaseRoleId = await LeaseRoleIdAsync("TENANT");
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        Guid leaseId;
        using (var createScope = _factory.Services.CreateScope())
        {
            var bll = createScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var invalidDate = await bll.Leases.CreateForResidentAsync(
                ResidentRoute(resident.IdCode),
                new LeaseBllDto
                {
                    UnitId = TestTenants.UnitAId,
                    LeaseRoleId = leaseRoleId,
                    StartDate = start,
                    EndDate = start.AddDays(-1)
                });
            var created = await bll.Leases.CreateForResidentAsync(
                ResidentRoute(resident.IdCode),
                new LeaseBllDto
                {
                    UnitId = TestTenants.UnitAId,
                    LeaseRoleId = leaseRoleId,
                    StartDate = start,
                    Notes = "Dependency lease"
                });
            var overlapping = await bll.Leases.CreateForResidentAsync(
                ResidentRoute(resident.IdCode),
                new LeaseBllDto
                {
                    UnitId = TestTenants.UnitAId,
                    LeaseRoleId = leaseRoleId,
                    StartDate = start.AddDays(1)
                });

            invalidDate.ShouldFailWith<ValidationAppError>();
            created.IsSuccess.Should().BeTrue();
            overlapping.ShouldFailWith<ConflictError>();
            leaseId = created.Value.Id;
        }

        using (var deleteBlockedScope = _factory.Services.CreateScope())
        {
            var bll = deleteBlockedScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var blocked = await bll.Residents.DeleteAsync(ResidentRoute(resident.IdCode), resident.IdCode);

            blocked.ShouldFailWith<BusinessRuleError>();
        }

        using (var cleanupScope = _factory.Services.CreateScope())
        {
            var bll = cleanupScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var deletedLease = await bll.Leases.DeleteFromResidentAsync(ResidentLeaseRoute(resident.IdCode, leaseId));
            var deletedResident = await bll.Residents.DeleteAsync(ResidentRoute(resident.IdCode), resident.IdCode);

            deletedLease.IsSuccess.Should().BeTrue();
            deletedResident.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task OptionsAreScopedAndLocalized()
    {
        using var culture = new CultureScope("en");
        var resident = await CreateResidentAsync("resident-options");

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var propertySearch = await bll.Leases.SearchPropertiesAsync(ResidentRoute(resident.IdCode), "Property A");
        var units = await bll.Leases.ListUnitsForPropertyAsync(ResidentRoute(resident.IdCode), TestTenants.PropertyAId);
        var roles = await bll.Leases.ListLeaseRolesAsync();

        propertySearch.Value.Properties.Should().Contain(property => property.PropertyId == TestTenants.PropertyAId);
        units.Value.Units.Should().Contain(unit => unit.UnitId == TestTenants.UnitAId);
        roles.Value.Roles.Should().Contain(role => role.Code == "TENANT" && role.Label == "Tenant");
    }

    private async Task<ResidentSeed> CreateResidentAsync(string suffix)
    {
        var idCode = $"BLL-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        var firstName = $"Resident{suffix}"[..Math.Min($"Resident{suffix}".Length, 30)];

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var created = await bll.Residents.CreateAndGetProfileAsync(
            CompanyRoute(),
            new ResidentBllDto
            {
                FirstName = firstName,
                LastName = "Workflow",
                IdCode = idCode,
                PreferredLanguage = "en"
            });

        created.IsSuccess.Should().BeTrue();
        return new ResidentSeed(created.Value.ResidentId, created.Value.ResidentIdCode, firstName, "Workflow");
    }

    private async Task<Guid> LeaseRoleIdAsync(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<App.DAL.EF.AppDbContext>();

        return await db.LeaseRoles
            .AsNoTracking()
            .Where(role => role.Code == code)
            .Select(role => role.Id)
            .SingleAsync();
    }

    private static ManagementCompanyRoute CompanyRoute()
    {
        return new ManagementCompanyRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug
        };
    }

    private static ResidentRoute ResidentRoute(string residentIdCode)
    {
        return new ResidentRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            ResidentIdCode = residentIdCode
        };
    }

    private static UnitRoute UnitRoute()
    {
        return new UnitRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            CustomerSlug = CustomerASlug,
            PropertySlug = PropertyASlug,
            UnitSlug = UnitASlug
        };
    }

    private static ResidentLeaseRoute ResidentLeaseRoute(string residentIdCode, Guid leaseId)
    {
        return new ResidentLeaseRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            ResidentIdCode = residentIdCode,
            LeaseId = leaseId
        };
    }

    private static UnitLeaseRoute UnitLeaseRoute(Guid leaseId)
    {
        return new UnitLeaseRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            CustomerSlug = CustomerASlug,
            PropertySlug = PropertyASlug,
            UnitSlug = UnitASlug,
            LeaseId = leaseId
        };
    }

    private sealed record ResidentSeed(Guid ResidentId, string IdCode, string FirstName, string LastName);
}
