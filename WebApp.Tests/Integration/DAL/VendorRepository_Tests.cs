using App.DAL.Contracts;
using App.DAL.DTO.Vendors;
using App.DAL.EF;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.DAL;

public class VendorRepository_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public VendorRepository_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AllByCompanyAsync_ReturnsTenantScopedVendorsWithCounts()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var vendors = await uow.Vendors.AllByCompanyAsync(TestTenants.CompanyAId);
        var missing = await uow.Vendors.AllByCompanyAsync(Guid.NewGuid());

        vendors.Should().ContainSingle(vendor => vendor.Id == TestTenants.VendorAId);
        vendors.Should().OnlyContain(vendor => vendor.ManagementCompanyId == TestTenants.CompanyAId);
        vendors[0].Name.Should().Be("Vendor A");
        vendors[0].AssignedTicketCount.Should().Be(1);
        vendors[0].ActiveCategoryCount.Should().Be(0);
        vendors[0].ContactCount.Should().Be(0);
        missing.Should().BeEmpty();
    }

    [Fact]
    public async Task FindProfileAsync_ReturnsVendorOnlyWithinCompany()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var profile = await uow.Vendors.FindProfileAsync(TestTenants.VendorAId, TestTenants.CompanyAId);
        var wrongCompany = await uow.Vendors.FindProfileAsync(TestTenants.VendorAId, Guid.NewGuid());

        profile.Should().NotBeNull();
        profile!.Id.Should().Be(TestTenants.VendorAId);
        profile.ManagementCompanyId.Should().Be(TestTenants.CompanyAId);
        profile.CompanySlug.Should().Be(TestTenants.CompanyASlug);
        profile.CompanyName.Should().Be(TestTenants.CompanyAName);
        profile.Name.Should().Be("Vendor A");
        profile.RegistryCode.Should().Be("TEST-VENDOR-A");
        profile.Notes.Should().Be("Seed vendor");
        profile.AssignedTicketCount.Should().Be(1);
        profile.ActiveCategoryCount.Should().Be(0);
        profile.ContactCount.Should().Be(0);
        profile.ScheduledWorkCount.Should().Be(0);
        wrongCompany.Should().BeNull();
    }

    [Fact]
    public async Task ExistenceAndRegistryQueries_AreTenantScoped()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var registryExists = await uow.Vendors.RegistryCodeExistsInCompanyAsync(TestTenants.CompanyAId, " test-vendor-a ");
        var registryExceptSelf = await uow.Vendors.RegistryCodeExistsInCompanyAsync(
            TestTenants.CompanyAId,
            "TEST-VENDOR-A",
            TestTenants.VendorAId);
        var existsInCompany = await uow.Vendors.ExistsInCompanyAsync(TestTenants.VendorAId, TestTenants.CompanyAId);
        var existsInWrongCompany = await uow.Vendors.ExistsInCompanyAsync(TestTenants.VendorAId, Guid.NewGuid());

        registryExists.Should().BeTrue();
        registryExceptSelf.Should().BeFalse();
        existsInCompany.Should().BeTrue();
        existsInWrongCompany.Should().BeFalse();
    }

    [Fact]
    public async Task OptionsForTicketAsync_AppliesCompanyAndCategoryFilters()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var companyOptions = await uow.Vendors.OptionsForTicketAsync(TestTenants.CompanyAId);
        var categoryOptions = await uow.Vendors.OptionsForTicketAsync(
            TestTenants.CompanyAId,
            TestTenants.TicketCategoryReferencedId);
        var wrongCompanyOptions = await uow.Vendors.OptionsForTicketAsync(Guid.NewGuid());

        companyOptions.Should().ContainSingle(option => option.Id == TestTenants.VendorAId && option.Label == "Vendor A");
        categoryOptions.Should().BeEmpty();
        wrongCompanyOptions.Should().BeEmpty();
    }

    [Fact]
    public async Task HasDeleteDependenciesAsync_DetectsAssignedTicketDependency()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var hasDependencies = await uow.Vendors.HasDeleteDependenciesAsync(TestTenants.VendorAId, TestTenants.CompanyAId);
        var wrongCompany = await uow.Vendors.HasDeleteDependenciesAsync(TestTenants.VendorAId, Guid.NewGuid());

        hasDependencies.Should().BeTrue();
        wrongCompany.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCurrentCultureNotesAndPreservesOtherTranslations()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var vendorId = Guid.NewGuid();

        uow.Vendors.Add(new VendorDalDto
        {
            Id = vendorId,
            ManagementCompanyId = TestTenants.CompanyAId,
            Name = "Update Vendor",
            RegistryCode = UniqueCode("VENDOR"),
            Notes = "English note"
        });
        await uow.SaveChangesAsync(CancellationToken.None);

        var entity = await db.Vendors.AsTracking().SingleAsync(vendor => vendor.Id == vendorId);
        entity.Notes.SetTranslation("Estonian note", "et");
        db.Entry(entity).Property(vendor => vendor.Notes).IsModified = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await uow.Vendors.UpdateAsync(new VendorDalDto
        {
            Id = vendorId,
            ManagementCompanyId = TestTenants.CompanyAId,
            Name = "Updated Vendor",
            RegistryCode = UniqueCode("VENDOR"),
            Notes = "English updated"
        }, TestTenants.CompanyAId);
        await uow.SaveChangesAsync(CancellationToken.None);

        var persisted = await db.Vendors.AsNoTracking().SingleAsync(vendor => vendor.Id == vendorId);
        persisted.Name.Should().Be("Updated Vendor");
        persisted.Notes.Translate("en").Should().Be("English updated");
        persisted.Notes.Translate("et").Should().Be("Estonian note");
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }
}
