using App.DAL.Contracts;
using App.DAL.DTO.Leases;
using App.DAL.DTO.Residents;
using App.DAL.EF;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.DAL;

public class LeaseRepository_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LeaseRepository_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListAndDetailsQueries_ReturnLeaseWithinResidentAndUnitScopes()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var resident = await CreateResidentAsync(uow, "List");
        var leaseRoleId = await TenantLeaseRoleIdAsync(db);
        var leaseId = await CreateLeaseAsync(uow, resident.Id, leaseRoleId, "Lease note");

        var byResident = await uow.Leases.AllByResidentAsync(resident.Id, TestTenants.CompanyAId);
        var byUnit = await uow.Leases.AllByUnitAsync(TestTenants.UnitAId, TestTenants.PropertyAId, TestTenants.CompanyAId);
        var residentDetails = await uow.Leases.FirstByIdForResidentAsync(leaseId, resident.Id, TestTenants.CompanyAId);
        var unitDetails = await uow.Leases.FirstByIdForUnitAsync(leaseId, TestTenants.UnitAId, TestTenants.PropertyAId, TestTenants.CompanyAId);
        var wrongResident = await uow.Leases.FirstByIdForResidentAsync(leaseId, Guid.NewGuid(), TestTenants.CompanyAId);

        byResident.Should().ContainSingle(lease => lease.LeaseId == leaseId);
        byResident[0].PropertyName.Should().Be("Property A");
        byResident[0].LeaseRoleCode.Should().Be("TENANT");
        byUnit.Should().Contain(lease => lease.LeaseId == leaseId && lease.ResidentFullName == "List Resident");
        residentDetails.Should().NotBeNull();
        residentDetails!.LeaseRoleId.Should().Be(leaseRoleId);
        unitDetails.Should().NotBeNull();
        unitDetails!.ResidentId.Should().Be(resident.Id);
        wrongResident.Should().BeNull();
    }

    [Fact]
    public async Task HasOverlappingActiveLeaseAsync_DetectsActiveLease()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var resident = await CreateResidentAsync(uow, "Overlap");
        var leaseRoleId = await TenantLeaseRoleIdAsync(db);
        var leaseId = await CreateLeaseAsync(uow, resident.Id, leaseRoleId, "Overlap note");

        var overlaps = await uow.Leases.HasOverlappingActiveLeaseAsync(
            resident.Id,
            TestTenants.UnitAId,
            DateOnly.FromDateTime(DateTime.UtcNow));
        var exceptSelf = await uow.Leases.HasOverlappingActiveLeaseAsync(
            resident.Id,
            TestTenants.UnitAId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            leaseId);

        overlaps.Should().BeTrue();
        exceptSelf.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateForResidentAsync_UpdatesCurrentCultureNotesAndPreservesOtherTranslations()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var resident = await CreateResidentAsync(uow, "Update");
        var leaseRoleId = await TenantLeaseRoleIdAsync(db);
        var leaseId = await CreateLeaseAsync(uow, resident.Id, leaseRoleId, "English note");
        var updatedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));

        var entity = await db.Leases.AsTracking().SingleAsync(lease => lease.Id == leaseId);
        entity.Notes!.SetTranslation("Estonian note", "et");
        db.Entry(entity).Property(lease => lease.Notes).IsModified = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var updated = await uow.Leases.UpdateForResidentAsync(
            resident.Id,
            TestTenants.CompanyAId,
            new LeaseDalDto
            {
                Id = leaseId,
                ResidentId = resident.Id,
                UnitId = TestTenants.UnitAId,
                LeaseRoleId = leaseRoleId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
                EndDate = updatedEndDate,
                Notes = "English updated"
            });
        await uow.SaveChangesAsync(CancellationToken.None);

        updated.Should().BeTrue();
        var persisted = await db.Leases.AsNoTracking().SingleAsync(lease => lease.Id == leaseId);
        persisted.EndDate.Should().Be(updatedEndDate);
        persisted.Notes!.Translate("en").Should().Be("English updated");
        persisted.Notes.Translate("et").Should().Be("Estonian note");
    }

    [Fact]
    public async Task UpdateAndDeleteForUnitAsync_AreScopedByUnitPropertyAndCompany()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var resident = await CreateResidentAsync(uow, "Unit");
        var leaseRoleId = await TenantLeaseRoleIdAsync(db);
        var leaseId = await CreateLeaseAsync(uow, resident.Id, leaseRoleId, "Unit note");

        var wrongPropertyUpdate = await uow.Leases.UpdateForUnitAsync(
            TestTenants.UnitAId,
            Guid.NewGuid(),
            TestTenants.CompanyAId,
            new LeaseDalDto { Id = leaseId, ResidentId = resident.Id, UnitId = TestTenants.UnitAId, LeaseRoleId = leaseRoleId, StartDate = DateOnly.FromDateTime(DateTime.UtcNow), Notes = "Wrong" });
        var updated = await uow.Leases.UpdateForUnitAsync(
            TestTenants.UnitAId,
            TestTenants.PropertyAId,
            TestTenants.CompanyAId,
            new LeaseDalDto { Id = leaseId, ResidentId = resident.Id, UnitId = TestTenants.UnitAId, LeaseRoleId = leaseRoleId, StartDate = DateOnly.FromDateTime(DateTime.UtcNow), Notes = "Updated by unit" });
        var wrongCompanyDelete = await uow.Leases.DeleteForUnitAsync(leaseId, TestTenants.UnitAId, TestTenants.PropertyAId, Guid.NewGuid());
        var deleted = await uow.Leases.DeleteForUnitAsync(leaseId, TestTenants.UnitAId, TestTenants.PropertyAId, TestTenants.CompanyAId);
        await uow.SaveChangesAsync(CancellationToken.None);

        wrongPropertyUpdate.Should().BeFalse();
        updated.Should().BeTrue();
        wrongCompanyDelete.Should().BeFalse();
        deleted.Should().BeTrue();
        var exists = await db.Leases.AsNoTracking().AnyAsync(lease => lease.Id == leaseId);
        exists.Should().BeFalse();
    }

    private static async Task<ResidentDalDto> CreateResidentAsync(IAppUOW uow, string suffix)
    {
        var resident = new ResidentDalDto
        {
            Id = Guid.NewGuid(),
            ManagementCompanyId = TestTenants.CompanyAId,
            FirstName = suffix,
            LastName = "Resident",
            IdCode = $"ID{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            PreferredLanguage = "en"
        };

        uow.Residents.Add(resident);
        await uow.SaveChangesAsync(CancellationToken.None);
        return resident;
    }

    private static async Task<Guid> TenantLeaseRoleIdAsync(AppDbContext db)
    {
        return await db.LeaseRoles
            .AsNoTracking()
            .Where(role => role.Code == "TENANT")
            .Select(role => role.Id)
            .SingleAsync();
    }

    private static async Task<Guid> CreateLeaseAsync(IAppUOW uow, Guid residentId, Guid leaseRoleId, string notes)
    {
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
}
