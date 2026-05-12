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

public class Property_Workflow_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public Property_Workflow_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateListProfileUpdateAndDeleteProperty_Workflow()
    {
        using var culture = new CultureScope("en");
        var customer = await CreateCustomerAsync("property-customer");
        var property = await CreatePropertyAsync(customer.Slug, "property-basic");

        using (var listScope = _factory.Services.CreateScope())
        {
            var bll = listScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var list = await bll.Properties.ListForCustomerAsync(CustomerRoute(customer.Slug));
            var profile = await bll.Properties.GetProfileAsync(PropertyRoute(customer.Slug, property.Slug));

            list.Value.Should().Contain(item => item.PropertyId == property.PropertyId);
            profile.Value.Name.Should().Be(property.Name);
            profile.Value.AddressLine.Should().Be("Workflow Street 1");
        }

        using (var updateScope = _factory.Services.CreateScope())
        {
            var bll = updateScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var updated = await bll.Properties.UpdateAndGetProfileAsync(
                PropertyRoute(customer.Slug, property.Slug),
                new PropertyBllDto
                {
                    Label = "Updated Workflow Property",
                    AddressLine = "Updated Property Street 1",
                    City = "Tartu",
                    PostalCode = "50111",
                    Notes = "Updated workflow property"
                });

            updated.IsSuccess.Should().BeTrue();
            updated.Value.Name.Should().Be("Updated Workflow Property");
            updated.Value.City.Should().Be("Tartu");
        }

        using var deleteScope = _factory.Services.CreateScope();
        var deleteBll = deleteScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var deleted = await deleteBll.Properties.DeleteAsync(
            PropertyRoute(customer.Slug, property.Slug),
            "Updated Workflow Property");

        deleted.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PropertyDeleteIsBlockedWhileUnitExists()
    {
        var customer = await CreateCustomerAsync("property-delete-blocked");
        var property = await CreatePropertyAsync(customer.Slug, "property-delete-blocked");

        using (var unitScope = _factory.Services.CreateScope())
        {
            var bll = unitScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var unit = await bll.Units.CreateAndGetProfileAsync(
                PropertyRoute(customer.Slug, property.Slug),
                new UnitBllDto
                {
                    UnitNr = $"WF-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                    FloorNr = 1,
                    SizeM2 = 48.5m
                });

            unit.IsSuccess.Should().BeTrue();
        }

        using var deleteScope = _factory.Services.CreateScope();
        var deleteBll = deleteScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var blocked = await deleteBll.Properties.DeleteAsync(
            PropertyRoute(customer.Slug, property.Slug),
            property.Name);

        blocked.ShouldFailWith<BusinessRuleError>();
    }

    [Fact]
    public async Task PropertyValidationAndWrongParentFailuresAreReturned()
    {
        var customer = await CreateCustomerAsync("property-validation");
        var property = await CreatePropertyAsync(customer.Slug, "property-validation");

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var invalid = await bll.Properties.CreateAsync(CustomerRoute(customer.Slug), new PropertyBllDto
        {
            PropertyTypeId = Guid.NewGuid(),
            Label = "Invalid Property",
            AddressLine = "Invalid Street 1",
            City = "Tallinn",
            PostalCode = "10111"
        });
        var wrongCustomer = await bll.Properties.GetProfileAsync(PropertyRoute("missing-customer", property.Slug));

        invalid.ShouldFailWith<ValidationAppError>();
        wrongCustomer.ShouldFailWith<NotFoundError>();
    }

    private async Task<CustomerSeed> CreateCustomerAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var registryCode = UniqueCode("CUST");
        var name = $"Workflow Customer {suffix} {Guid.NewGuid():N}"[..45];
        var created = await bll.Customers.CreateAndGetProfileAsync(
            CompanyRoute(),
            new CustomerBllDto
            {
                Name = name,
                RegistryCode = registryCode,
                BillingEmail = $"{registryCode.ToLowerInvariant()}@test.ee",
                BillingAddress = "Workflow Customer Street 1",
                Phone = "+372 5555 7000"
            });

        created.IsSuccess.Should().BeTrue();
        return new CustomerSeed(created.Value.Slug);
    }

    private async Task<PropertySeed> CreatePropertyAsync(string customerSlug, string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var name = $"Workflow Property {suffix} {Guid.NewGuid():N}"[..45];
        var created = await bll.Properties.CreateAndGetProfileAsync(
            CustomerRoute(customerSlug),
            new PropertyBllDto
            {
                PropertyTypeId = TestTenants.PropertyTypeReferencedId,
                Label = name,
                AddressLine = "Workflow Street 1",
                City = "Tallinn",
                PostalCode = "10111",
                Notes = "Workflow property"
            });

        created.IsSuccess.Should().BeTrue();
        return new PropertySeed(created.Value.PropertyId, created.Value.PropertySlug, name);
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

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }

    private sealed record CustomerSeed(string Slug);
    private sealed record PropertySeed(Guid PropertyId, string Slug, string Name);
}
