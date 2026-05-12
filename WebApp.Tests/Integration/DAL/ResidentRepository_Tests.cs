using App.DAL.Contracts;
using App.DAL.DTO.Leases;
using App.DAL.DTO.Residents;
using App.DAL.EF;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.DAL;

public class ResidentRepository_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ResidentRepository_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProfileAndListQueries_ReturnResidentWithinCompany()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var resident = await CreateResidentAsync(uow, "Profile");

        var byRoute = await uow.Residents.FirstProfileAsync(TestTenants.CompanyASlug, resident.IdCode);
        var byIds = await uow.Residents.FindProfileAsync(resident.Id, TestTenants.CompanyAId);
        var list = await uow.Residents.AllByCompanyAsync(TestTenants.CompanyAId);
        var wrongCompany = await uow.Residents.FindProfileAsync(resident.Id, Guid.NewGuid());

        byRoute.Should().NotBeNull();
        byIds.Should().NotBeNull();
        byRoute!.Id.Should().Be(resident.Id);
        byRoute.CompanySlug.Should().Be(TestTenants.CompanyASlug);
        byIds!.PreferredLanguage.Should().Be("en");
        list.Should().Contain(item => item.Id == resident.Id);
        wrongCompany.Should().BeNull();
    }

    [Fact]
    public async Task ExistenceAndIdCodeQueries_AreCompanyScoped()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var resident = await CreateResidentAsync(uow, "Exists");

        var idCodeExists = await uow.Residents.IdCodeExistsForCompanyAsync(TestTenants.CompanyAId, $" {resident.IdCode.ToLowerInvariant()} ");
        var idCodeExceptSelf = await uow.Residents.IdCodeExistsForCompanyAsync(
            TestTenants.CompanyAId,
            resident.IdCode,
            resident.Id);
        var existsInCompany = await uow.Residents.ExistsInCompanyAsync(resident.Id, TestTenants.CompanyAId);
        var existsInWrongCompany = await uow.Residents.ExistsInCompanyAsync(resident.Id, Guid.NewGuid());

        idCodeExists.Should().BeTrue();
        idCodeExceptSelf.Should().BeFalse();
        existsInCompany.Should().BeTrue();
        existsInWrongCompany.Should().BeFalse();
    }

    [Fact]
    public async Task LeaseLinkedQueries_ReflectActiveLease()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var resident = await CreateResidentAsync(uow, "Lease");
        var leaseId = await CreateLeaseAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), uow, resident.Id, "Lease note");

        var linked = await uow.Residents.IsLinkedToUnitAsync(resident.Id, TestTenants.UnitAId);
        var options = await uow.Residents.OptionsForTicketAsync(TestTenants.CompanyAId, TestTenants.UnitAId);
        var leases = await uow.Residents.LeaseSummariesByResidentAsync(resident.Id);

        linked.Should().BeTrue();
        options.Should().Contain(option => option.Id == resident.Id && option.Label == "Lease Resident");
        leases.Should().ContainSingle(lease => lease.LeaseId == leaseId);
        leases[0].PropertyName.Should().Be("Property A");
        leases[0].UnitNr.Should().Be("A-101");
        leases[0].LeaseRoleCode.Should().Be("TENANT");
    }

    [Fact]
    public async Task HasDeleteDependenciesAsync_DetectsLeaseDependency()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var resident = await CreateResidentAsync(uow, "Delete");
        await CreateLeaseAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), uow, resident.Id, "Delete dependency");

        var hasDependencies = await uow.Residents.HasDeleteDependenciesAsync(resident.Id, TestTenants.CompanyAId);
        var wrongCompany = await uow.Residents.HasDeleteDependenciesAsync(resident.Id, Guid.NewGuid());

        hasDependencies.Should().BeTrue();
        wrongCompany.Should().BeFalse();
    }

    [Fact]
    public async Task ContactsByResidentAsync_ReturnsEmptyWhenNoContactsAssigned()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var resident = await CreateResidentAsync(uow, "Contacts");

        var contacts = await uow.Residents.ContactsByResidentAsync(resident.Id);

        contacts.Should().BeEmpty();
    }

    private static async Task<ResidentDalDto> CreateResidentAsync(IAppUOW uow, string suffix)
    {
        var resident = new ResidentDalDto
        {
            Id = Guid.NewGuid(),
            ManagementCompanyId = TestTenants.CompanyAId,
            FirstName = suffix,
            LastName = "Resident",
            IdCode = UniqueIdCode(),
            PreferredLanguage = "en"
        };

        uow.Residents.Add(resident);
        await uow.SaveChangesAsync(CancellationToken.None);
        return resident;
    }

    private static async Task<Guid> CreateLeaseAsync(AppDbContext db, IAppUOW uow, Guid residentId, string notes)
    {
        var leaseRoleId = await db.LeaseRoles
            .AsNoTracking()
            .Where(role => role.Code == "TENANT")
            .Select(role => role.Id)
            .SingleAsync();
        var id = Guid.NewGuid();

        uow.Leases.Add(new LeaseDalDto
        {
            Id = id,
            ResidentId = residentId,
            UnitId = TestTenants.UnitAId,
            LeaseRoleId = leaseRoleId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            Notes = notes
        });
        await uow.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static string UniqueIdCode()
    {
        return $"ID{Guid.NewGuid():N}"[..20].ToUpperInvariant();
    }
}
