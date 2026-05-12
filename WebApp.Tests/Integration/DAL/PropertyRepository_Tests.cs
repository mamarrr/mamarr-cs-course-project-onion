using App.DAL.Contracts;
using App.DAL.DTO.Properties;
using App.DAL.EF;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.DAL;

public class PropertyRepository_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PropertyRepository_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AllByCustomerAsync_ReturnsPropertiesForCustomer()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var properties = await uow.Properties.AllByCustomerAsync(TestTenants.CustomerAId);

        properties.Should().ContainSingle(property => property.Id == TestTenants.PropertyAId);
        properties.Should().OnlyContain(property => property.CustomerId == TestTenants.CustomerAId);
        properties[0].Name.Should().Be("Property A");
        properties[0].PropertyTypeCode.Should().Be("APARTMENT_BUILDING");
    }

    [Fact]
    public async Task FirstWorkspaceByCustomerAndSlugAsync_ReturnsPropertyWithinCustomer()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var workspace = await uow.Properties.FirstWorkspaceByCustomerAndSlugAsync(
            TestTenants.CustomerAId,
            " property-a ");
        var wrongCustomer = await uow.Properties.FirstWorkspaceByCustomerAndSlugAsync(Guid.NewGuid(), "property-a");

        workspace.Should().NotBeNull();
        workspace!.Id.Should().Be(TestTenants.PropertyAId);
        workspace.CustomerId.Should().Be(TestTenants.CustomerAId);
        workspace.Name.Should().Be("Property A");
        workspace.Slug.Should().Be("property-a");
        wrongCustomer.Should().BeNull();
    }

    [Fact]
    public async Task FindProfileAsync_ReturnsPropertyOnlyWithinCustomer()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var profile = await uow.Properties.FindProfileAsync(TestTenants.PropertyAId, TestTenants.CustomerAId);
        var wrongCustomer = await uow.Properties.FindProfileAsync(TestTenants.PropertyAId, Guid.NewGuid());

        profile.Should().NotBeNull();
        profile!.ManagementCompanyId.Should().Be(TestTenants.CompanyAId);
        profile.CompanySlug.Should().Be(TestTenants.CompanyASlug);
        profile.CustomerSlug.Should().Be("customer-a");
        profile.Name.Should().Be("Property A");
        profile.PropertyTypeCode.Should().Be("APARTMENT_BUILDING");
        wrongCustomer.Should().BeNull();
    }

    [Fact]
    public async Task ExistenceAndSlugQueries_AreScopedToCustomerAndCompany()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var slugExists = await uow.Properties.SlugExistsForCustomerAsync(TestTenants.CustomerAId, " PROPERTY-A ");
        var slugExceptSelf = await uow.Properties.SlugExistsForCustomerAsync(
            TestTenants.CustomerAId,
            "property-a",
            TestTenants.PropertyAId);
        var existsInCompany = await uow.Properties.ExistsInCompanyAsync(TestTenants.PropertyAId, TestTenants.CompanyAId);
        var existsInWrongCompany = await uow.Properties.ExistsInCompanyAsync(TestTenants.PropertyAId, Guid.NewGuid());
        var existsInCustomer = await uow.Properties.ExistsInCustomerAsync(TestTenants.PropertyAId, TestTenants.CustomerAId);
        var existsInWrongCustomer = await uow.Properties.ExistsInCustomerAsync(TestTenants.PropertyAId, Guid.NewGuid());

        slugExists.Should().BeTrue();
        slugExceptSelf.Should().BeFalse();
        existsInCompany.Should().BeTrue();
        existsInWrongCompany.Should().BeFalse();
        existsInCustomer.Should().BeTrue();
        existsInWrongCustomer.Should().BeFalse();
    }

    [Fact]
    public async Task OptionsForTicketAsync_ReturnsCompanyAndCustomerScopedProperties()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var companyOptions = await uow.Properties.OptionsForTicketAsync(TestTenants.CompanyAId);
        var customerOptions = await uow.Properties.OptionsForTicketAsync(TestTenants.CompanyAId, TestTenants.CustomerAId);
        var wrongCustomerOptions = await uow.Properties.OptionsForTicketAsync(TestTenants.CompanyAId, Guid.NewGuid());

        companyOptions.Should().ContainSingle(option => option.Id == TestTenants.PropertyAId && option.Label == "Property A");
        customerOptions.Should().ContainSingle(option => option.Id == TestTenants.PropertyAId && option.Label == "Property A");
        wrongCustomerOptions.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchForLeaseAssignmentAsync_SearchesWithinCompany()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var matches = await uow.Properties.SearchForLeaseAssignmentAsync(TestTenants.CompanyAId, "Property");
        var noSearch = await uow.Properties.SearchForLeaseAssignmentAsync(TestTenants.CompanyAId, " ");
        var wrongCompany = await uow.Properties.SearchForLeaseAssignmentAsync(Guid.NewGuid(), "Property");

        matches.Should().ContainSingle(match => match.PropertyId == TestTenants.PropertyAId);
        matches[0].CustomerId.Should().Be(TestTenants.CustomerAId);
        matches[0].PropertySlug.Should().Be("property-a");
        noSearch.Should().BeEmpty();
        wrongCompany.Should().BeEmpty();
    }

    [Fact]
    public async Task HasDeleteDependenciesAsync_DetectsSeededPropertyDependencies()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var hasDependencies = await uow.Properties.HasDeleteDependenciesAsync(
            TestTenants.PropertyAId,
            TestTenants.CustomerAId,
            TestTenants.CompanyAId);
        var wrongCustomer = await uow.Properties.HasDeleteDependenciesAsync(
            TestTenants.PropertyAId,
            Guid.NewGuid(),
            TestTenants.CompanyAId);

        hasDependencies.Should().BeTrue();
        wrongCustomer.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCurrentCultureFieldsAndPreservesOtherTranslations()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var propertyId = Guid.NewGuid();
        var slug = UniqueSlug("property-update");

        uow.Properties.Add(new PropertyDalDto
        {
            Id = propertyId,
            CustomerId = TestTenants.CustomerAId,
            PropertyTypeId = TestTenants.PropertyTypeReferencedId,
            Label = "English property",
            Slug = slug,
            AddressLine = "Original Address 1",
            City = "Tallinn",
            PostalCode = "10111",
            Notes = "English note"
        });
        await uow.SaveChangesAsync(CancellationToken.None);

        var entity = await db.Properties.AsTracking().SingleAsync(property => property.Id == propertyId);
        entity.Label.SetTranslation("Eesti kinnistu", "et");
        entity.Notes!.SetTranslation("Eesti märkus", "et");
        db.Entry(entity).Property(property => property.Label).IsModified = true;
        db.Entry(entity).Property(property => property.Notes).IsModified = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await uow.Properties.UpdateAsync(new PropertyDalDto
        {
            Id = propertyId,
            CustomerId = TestTenants.CustomerAId,
            PropertyTypeId = TestTenants.PropertyTypeReferencedId,
            Label = "English property updated",
            Slug = slug,
            AddressLine = "Updated Address 1",
            City = "Tartu",
            PostalCode = "50111",
            Notes = "English note updated"
        }, TestTenants.CustomerAId);
        await uow.SaveChangesAsync(CancellationToken.None);

        var persisted = await db.Properties.AsNoTracking().SingleAsync(property => property.Id == propertyId);
        persisted.AddressLine.Should().Be("Updated Address 1");
        persisted.City.Should().Be("Tartu");
        persisted.Label.Translate("en").Should().Be("English property updated");
        persisted.Label.Translate("et").Should().Be("Eesti kinnistu");
        persisted.Notes!.Translate("en").Should().Be("English note updated");
        persisted.Notes.Translate("et").Should().Be("Eesti märkus");
    }

    private static string UniqueSlug(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32];
    }
}
