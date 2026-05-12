using App.DAL.Contracts;
using App.DAL.DTO.Units;
using App.DAL.EF;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.DAL;

public class UnitRepository_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UnitRepository_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FirstDashboardAsync_ReturnsUnitWithinFullRouteScope()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var dashboard = await uow.Units.FirstDashboardAsync(" company-a ", " customer-a ", " property-a ", " a-101 ");
        var wrongRoute = await uow.Units.FirstDashboardAsync("company-a", "customer-a", "missing-property", "a-101");

        dashboard.Should().NotBeNull();
        dashboard!.Id.Should().Be(TestTenants.UnitAId);
        dashboard.ManagementCompanyId.Should().Be(TestTenants.CompanyAId);
        dashboard.CustomerId.Should().Be(TestTenants.CustomerAId);
        dashboard.PropertyId.Should().Be(TestTenants.PropertyAId);
        dashboard.PropertyName.Should().Be("Property A");
        dashboard.UnitNr.Should().Be("A-101");
        wrongRoute.Should().BeNull();
    }

    [Fact]
    public async Task ProfileQueries_ReturnUnitOnlyWithinParentScope()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var byRoute = await uow.Units.FirstProfileAsync("company-a", "customer-a", "property-a", "a-101");
        var byIds = await uow.Units.FindProfileAsync(TestTenants.UnitAId, TestTenants.PropertyAId);
        var wrongProperty = await uow.Units.FindProfileAsync(TestTenants.UnitAId, Guid.NewGuid());

        byRoute.Should().NotBeNull();
        byIds.Should().NotBeNull();
        byRoute!.Id.Should().Be(TestTenants.UnitAId);
        byRoute.CompanySlug.Should().Be(TestTenants.CompanyASlug);
        byRoute.CustomerSlug.Should().Be("customer-a");
        byRoute.PropertySlug.Should().Be("property-a");
        byIds!.Slug.Should().Be("a-101");
        wrongProperty.Should().BeNull();
    }

    [Fact]
    public async Task ListAndFindMethods_AreScopedByProperty()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var all = (await uow.Units.AllAsync(TestTenants.PropertyAId)).ToList();
        var listItems = await uow.Units.AllByPropertyAsync(TestTenants.PropertyAId);
        var found = await uow.Units.FindAsync(TestTenants.UnitAId, TestTenants.PropertyAId);
        var wrongProperty = await uow.Units.FindAsync(TestTenants.UnitAId, Guid.NewGuid());

        all.Should().ContainSingle(unit => unit.Id == TestTenants.UnitAId);
        listItems.Should().ContainSingle(unit => unit.Id == TestTenants.UnitAId);
        listItems[0].UnitNr.Should().Be("A-101");
        found.Should().NotBeNull();
        found!.Slug.Should().Be("a-101");
        wrongProperty.Should().BeNull();
    }

    [Fact]
    public async Task ExistenceAndSlugQueries_AreScopedToPropertyAndCompany()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var matchingSlugs = await uow.Units.AllSlugsByPropertyWithPrefixAsync(TestTenants.PropertyAId, " a- ");
        var slugExists = await uow.Units.UnitSlugExistsForPropertyAsync(TestTenants.PropertyAId, " A-101 ");
        var slugExceptSelf = await uow.Units.UnitSlugExistsForPropertyAsync(
            TestTenants.PropertyAId,
            "a-101",
            TestTenants.UnitAId);
        var existsInCompany = await uow.Units.ExistsInCompanyAsync(TestTenants.UnitAId, TestTenants.CompanyAId);
        var existsInWrongCompany = await uow.Units.ExistsInCompanyAsync(TestTenants.UnitAId, Guid.NewGuid());
        var existsInProperty = await uow.Units.ExistsInPropertyAsync(TestTenants.UnitAId, TestTenants.PropertyAId);
        var existsInWrongProperty = await uow.Units.ExistsInPropertyAsync(TestTenants.UnitAId, Guid.NewGuid());

        matchingSlugs.Should().Contain("a-101");
        slugExists.Should().BeTrue();
        slugExceptSelf.Should().BeFalse();
        existsInCompany.Should().BeTrue();
        existsInWrongCompany.Should().BeFalse();
        existsInProperty.Should().BeTrue();
        existsInWrongProperty.Should().BeFalse();
    }

    [Fact]
    public async Task OptionsForTicketAsync_AppliesCompanyPropertyAndCustomerFilters()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var companyOptions = await uow.Units.OptionsForTicketAsync(TestTenants.CompanyAId);
        var propertyOptions = await uow.Units.OptionsForTicketAsync(TestTenants.CompanyAId, TestTenants.PropertyAId);
        var customerOptions = await uow.Units.OptionsForTicketAsync(TestTenants.CompanyAId, customerId: TestTenants.CustomerAId);
        var wrongProperty = await uow.Units.OptionsForTicketAsync(TestTenants.CompanyAId, Guid.NewGuid());

        companyOptions.Should().ContainSingle(option => option.Id == TestTenants.UnitAId && option.Label == "A-101");
        propertyOptions.Should().ContainSingle(option => option.Id == TestTenants.UnitAId && option.Label == "A-101");
        customerOptions.Should().ContainSingle(option => option.Id == TestTenants.UnitAId && option.Label == "A-101");
        wrongProperty.Should().BeEmpty();
    }

    [Fact]
    public async Task ListForLeaseAssignmentAsync_ReturnsUnitsWithinCompanyProperty()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var options = await uow.Units.ListForLeaseAssignmentAsync(TestTenants.PropertyAId, TestTenants.CompanyAId);
        var wrongCompany = await uow.Units.ListForLeaseAssignmentAsync(TestTenants.PropertyAId, Guid.NewGuid());

        options.Should().ContainSingle(option => option.UnitId == TestTenants.UnitAId);
        options[0].UnitSlug.Should().Be("a-101");
        options[0].UnitNr.Should().Be("A-101");
        wrongCompany.Should().BeEmpty();
    }

    [Fact]
    public async Task HasDeleteDependenciesAsync_DetectsSeededUnitDependencies()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var hasDependencies = await uow.Units.HasDeleteDependenciesAsync(
            TestTenants.UnitAId,
            TestTenants.PropertyAId,
            TestTenants.CompanyAId);
        var wrongProperty = await uow.Units.HasDeleteDependenciesAsync(
            TestTenants.UnitAId,
            Guid.NewGuid(),
            TestTenants.CompanyAId);

        hasDependencies.Should().BeTrue();
        wrongProperty.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCurrentCultureNotesAndPreservesOtherTranslations()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var unitId = Guid.NewGuid();
        var slug = UniqueSlug("unit-update");

        uow.Units.Add(new UnitDalDto
        {
            Id = unitId,
            PropertyId = TestTenants.PropertyAId,
            UnitNr = "U-900",
            Slug = slug,
            FloorNr = 9,
            SizeM2 = 90,
            Notes = "English note"
        });
        await uow.SaveChangesAsync(CancellationToken.None);

        var entity = await db.Units.AsTracking().SingleAsync(unit => unit.Id == unitId);
        entity.Notes!.SetTranslation("Estonian note", "et");
        db.Entry(entity).Property(unit => unit.Notes).IsModified = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await uow.Units.UpdateAsync(new UnitDalDto
        {
            Id = unitId,
            PropertyId = TestTenants.PropertyAId,
            UnitNr = "U-901",
            Slug = slug,
            FloorNr = 10,
            SizeM2 = 91,
            Notes = "English updated"
        }, TestTenants.PropertyAId);
        await uow.SaveChangesAsync(CancellationToken.None);

        var persisted = await db.Units.AsNoTracking().SingleAsync(unit => unit.Id == unitId);
        persisted.UnitNr.Should().Be("U-901");
        persisted.FloorNr.Should().Be(10);
        persisted.SizeM2.Should().Be(91m);
        persisted.Notes!.Translate("en").Should().Be("English updated");
        persisted.Notes.Translate("et").Should().Be("Estonian note");
    }

    private static string UniqueSlug(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32];
    }
}
