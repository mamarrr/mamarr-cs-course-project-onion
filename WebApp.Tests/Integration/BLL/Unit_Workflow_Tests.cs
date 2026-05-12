using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Customers;
using App.BLL.DTO.Properties;
using App.BLL.DTO.Units;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.BLL;

public class Unit_Workflow_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public Unit_Workflow_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateListProfileUpdateAndDeleteUnit_Workflow()
    {
        var hierarchy = await CreatePropertyHierarchyAsync("unit-basic");
        var unit = await CreateUnitAsync(hierarchy.CustomerSlug, hierarchy.PropertySlug, "unit-basic");

        using (var listScope = _factory.Services.CreateScope())
        {
            var bll = listScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var list = await bll.Units.ListForPropertyAsync(PropertyRoute(hierarchy.CustomerSlug, hierarchy.PropertySlug));
            var profile = await bll.Units.GetProfileAsync(UnitRoute(hierarchy.CustomerSlug, hierarchy.PropertySlug, unit.Slug));

            list.Value.Units.Should().Contain(item => item.UnitId == unit.UnitId);
            profile.Value.UnitNr.Should().Be(unit.UnitNr);
        }

        using (var updateScope = _factory.Services.CreateScope())
        {
            var bll = updateScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var updated = await bll.Units.UpdateAndGetProfileAsync(
                UnitRoute(hierarchy.CustomerSlug, hierarchy.PropertySlug, unit.Slug),
                new UnitBllDto
                {
                    UnitNr = "WF-201",
                    FloorNr = 2,
                    SizeM2 = 62.5m,
                    Notes = "Updated workflow unit"
                });

            updated.IsSuccess.Should().BeTrue();
            updated.Value.UnitNr.Should().Be("WF-201");
            updated.Value.FloorNr.Should().Be(2);
            updated.Value.SizeM2.Should().Be(62.5m);
        }

        using var deleteScope = _factory.Services.CreateScope();
        var deleteBll = deleteScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var deleted = await deleteBll.Units.DeleteAsync(
            UnitRoute(hierarchy.CustomerSlug, hierarchy.PropertySlug, unit.Slug),
            "WF-201");

        deleted.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UnitValidationAndWrongParentFailuresAreReturned()
    {
        var hierarchy = await CreatePropertyHierarchyAsync("unit-validation");
        var unit = await CreateUnitAsync(hierarchy.CustomerSlug, hierarchy.PropertySlug, "unit-validation");

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var invalid = await bll.Units.CreateAsync(PropertyRoute(hierarchy.CustomerSlug, hierarchy.PropertySlug), new UnitBllDto
        {
            UnitNr = " ",
            FloorNr = 1,
            SizeM2 = 40
        });
        var wrongProperty = await bll.Units.GetProfileAsync(UnitRoute(hierarchy.CustomerSlug, "missing-property", unit.Slug));

        invalid.ShouldFailWith<ValidationAppError>();
        wrongProperty.ShouldFailWith<NotFoundError>();
    }

    private async Task<PropertyHierarchy> CreatePropertyHierarchyAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var registryCode = UniqueCode("CUST");
        var customer = await bll.Customers.CreateAndGetProfileAsync(
            CompanyRoute(),
            new CustomerBllDto
            {
                Name = $"Workflow Customer {suffix} {Guid.NewGuid():N}"[..45],
                RegistryCode = registryCode,
                BillingEmail = $"{registryCode.ToLowerInvariant()}@test.ee",
                BillingAddress = "Workflow Customer Street 1",
                Phone = "+372 5555 7000"
            });
        customer.IsSuccess.Should().BeTrue();

        var property = await bll.Properties.CreateAndGetProfileAsync(
            CustomerRoute(customer.Value.Slug),
            new PropertyBllDto
            {
                PropertyTypeId = TestTenants.PropertyTypeReferencedId,
                Label = $"Workflow Property {suffix} {Guid.NewGuid():N}"[..45],
                AddressLine = "Workflow Street 1",
                City = "Tallinn",
                PostalCode = "10111"
            });
        property.IsSuccess.Should().BeTrue();

        return new PropertyHierarchy(customer.Value.Slug, property.Value.PropertySlug);
    }

    private async Task<UnitSeed> CreateUnitAsync(string customerSlug, string propertySlug, string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var unitNr = $"WF-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var created = await bll.Units.CreateAndGetProfileAsync(
            PropertyRoute(customerSlug, propertySlug),
            new UnitBllDto
            {
                UnitNr = unitNr,
                FloorNr = 1,
                SizeM2 = 48.5m,
                Notes = $"Workflow unit {suffix}"
            });

        created.IsSuccess.Should().BeTrue();
        return new UnitSeed(created.Value.UnitId, created.Value.UnitSlug, unitNr);
    }

    private static ManagementCompanyRoute CompanyRoute()
    {
        return new ManagementCompanyRoute { AppUserId = TestUsers.CompanyAOwnerId, CompanySlug = TestTenants.CompanyASlug };
    }

    private static CustomerRoute CustomerRoute(string customerSlug)
    {
        return new CustomerRoute { AppUserId = TestUsers.CompanyAOwnerId, CompanySlug = TestTenants.CompanyASlug, CustomerSlug = customerSlug };
    }

    private static PropertyRoute PropertyRoute(string customerSlug, string propertySlug)
    {
        return new PropertyRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug
        };
    }

    private static UnitRoute UnitRoute(string customerSlug, string propertySlug, string unitSlug)
    {
        return new UnitRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug
        };
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }

    private sealed record PropertyHierarchy(string CustomerSlug, string PropertySlug);
    private sealed record UnitSeed(Guid UnitId, string Slug, string UnitNr);
}
