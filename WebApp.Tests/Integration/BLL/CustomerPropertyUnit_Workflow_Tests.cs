using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Customers;
using App.BLL.DTO.Customers.Errors;
using App.BLL.DTO.Properties;
using App.BLL.DTO.Units;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.BLL;

public class CustomerPropertyUnit_Workflow_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CustomerPropertyUnit_Workflow_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateHierarchy_ListsAndProfilesEachLevel()
    {
        using var culture = new CultureScope("en");
        var hierarchy = await CreateHierarchyAsync("list-profile");

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var customers = await bll.Customers.ListForCompanyAsync(CompanyRoute());
        var properties = await bll.Properties.ListForCustomerAsync(CustomerRoute(hierarchy.CustomerSlug));
        var units = await bll.Units.ListForPropertyAsync(PropertyRoute(hierarchy.CustomerSlug, hierarchy.PropertySlug));
        var customerProfile = await bll.Customers.GetProfileAsync(CustomerRoute(hierarchy.CustomerSlug));
        var propertyProfile = await bll.Properties.GetProfileAsync(PropertyRoute(hierarchy.CustomerSlug, hierarchy.PropertySlug));
        var unitProfile = await bll.Units.GetProfileAsync(UnitRoute(hierarchy.CustomerSlug, hierarchy.PropertySlug, hierarchy.UnitSlug));

        customers.Value.Should().Contain(customer => customer.CustomerId == hierarchy.CustomerId);
        properties.Value.Should().Contain(property => property.PropertyId == hierarchy.PropertyId);
        units.Value.Units.Should().Contain(unit => unit.UnitId == hierarchy.UnitId);
        customerProfile.Value.Name.Should().Be(hierarchy.CustomerName);
        propertyProfile.Value.Name.Should().Be(hierarchy.PropertyName);
        unitProfile.Value.UnitNr.Should().Be(hierarchy.UnitNr);
    }

    [Fact]
    public async Task UpdateHierarchy_PersistsChangesAtEachLevel()
    {
        using var culture = new CultureScope("en");
        var hierarchy = await CreateHierarchyAsync("update");

        var updatedCustomer = await UpdateCustomerAsync(hierarchy.CustomerSlug, hierarchy.CustomerRegistryCode);
        var updatedProperty = await UpdatePropertyAsync(hierarchy.CustomerSlug, hierarchy.PropertySlug);
        var updatedUnit = await UpdateUnitAsync(hierarchy.CustomerSlug, hierarchy.PropertySlug, hierarchy.UnitSlug);

        updatedCustomer.Name.Should().Be("Updated Workflow Customer");
        updatedCustomer.BillingEmail.Should().Be("updated-workflow-customer@test.ee");
        updatedProperty.Name.Should().Be("Updated Workflow Property");
        updatedProperty.City.Should().Be("Tartu");
        updatedUnit.UnitNr.Should().Be("WF-201");
        updatedUnit.FloorNr.Should().Be(2);
        updatedUnit.SizeM2.Should().Be(62.5m);
    }

    [Fact]
    public async Task DeleteHierarchy_BlocksParentsUntilChildrenAreRemoved()
    {
        var hierarchy = await CreateHierarchyAsync("delete");

        using (var blockedScope = _factory.Services.CreateScope())
        {
            var bll = blockedScope.ServiceProvider.GetRequiredService<IAppBLL>();

            var propertyBlocked = await bll.Properties.DeleteAsync(
                PropertyRoute(hierarchy.CustomerSlug, hierarchy.PropertySlug),
                hierarchy.PropertyName);
            var customerBlocked = await bll.Customers.DeleteAsync(
                CustomerRoute(hierarchy.CustomerSlug),
                hierarchy.CustomerName);

            propertyBlocked.ShouldFailWith<BusinessRuleError>();
            customerBlocked.ShouldFailWith<BusinessRuleError>();
        }

        using (var unitScope = _factory.Services.CreateScope())
        {
            var bll = unitScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var deletedUnit = await bll.Units.DeleteAsync(
                UnitRoute(hierarchy.CustomerSlug, hierarchy.PropertySlug, hierarchy.UnitSlug),
                hierarchy.UnitNr);

            deletedUnit.IsSuccess.Should().BeTrue();
        }

        using (var propertyScope = _factory.Services.CreateScope())
        {
            var bll = propertyScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var deletedProperty = await bll.Properties.DeleteAsync(
                PropertyRoute(hierarchy.CustomerSlug, hierarchy.PropertySlug),
                hierarchy.PropertyName);

            deletedProperty.IsSuccess.Should().BeTrue();
        }

        using (var customerScope = _factory.Services.CreateScope())
        {
            var bll = customerScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var deletedCustomer = await bll.Customers.DeleteAsync(
                CustomerRoute(hierarchy.CustomerSlug),
                hierarchy.CustomerName);

            deletedCustomer.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task CrossCompanyAndMissingRoutesFailWithoutLeakingSeededHierarchy()
    {
        var hierarchy = await CreateHierarchyAsync("route-fail");

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var customerWrongCompany = await bll.Customers.GetProfileAsync(new CustomerRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = "missing-company",
            CustomerSlug = hierarchy.CustomerSlug
        });
        var propertyWrongCustomer = await bll.Properties.GetProfileAsync(PropertyRoute("missing-customer", hierarchy.PropertySlug));
        var unitWrongProperty = await bll.Units.GetProfileAsync(UnitRoute(hierarchy.CustomerSlug, "missing-property", hierarchy.UnitSlug));

        customerWrongCompany.ShouldFailWith<NotFoundError>();
        propertyWrongCustomer.ShouldFailWith<NotFoundError>();
        unitWrongProperty.ShouldFailWith<NotFoundError>();
    }

    [Fact]
    public async Task ValidationAndDuplicateRegistryFailuresAreReturnedAsBusinessResults()
    {
        var hierarchy = await CreateHierarchyAsync("validation");

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var duplicateCustomer = await bll.Customers.CreateAsync(CompanyRoute(), NewCustomerDto(
            "Duplicate Workflow Customer",
            hierarchy.CustomerRegistryCode));
        var invalidProperty = await bll.Properties.CreateAsync(CustomerRoute(hierarchy.CustomerSlug), new PropertyBllDto
        {
            PropertyTypeId = Guid.NewGuid(),
            Label = "Invalid Property",
            AddressLine = "Invalid Street 1",
            City = "Tallinn",
            PostalCode = "10111"
        });
        var invalidUnit = await bll.Units.CreateAsync(PropertyRoute(hierarchy.CustomerSlug, hierarchy.PropertySlug), new UnitBllDto
        {
            UnitNr = " ",
            FloorNr = 1,
            SizeM2 = 40
        });

        duplicateCustomer.IsFailed.Should().BeTrue();
        duplicateCustomer.Errors.Should().Contain(error => error is DuplicateRegistryCodeError);
        invalidProperty.ShouldFailWith<ValidationAppError>();
        invalidUnit.ShouldFailWith<ValidationAppError>();
    }

    private async Task<Hierarchy> CreateHierarchyAsync(string suffix)
    {
        var customerName = $"Workflow Customer {suffix} {Guid.NewGuid():N}"[..45];
        var propertyName = $"Workflow Property {suffix} {Guid.NewGuid():N}"[..45];
        var unitNr = $"WF-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var registryCode = UniqueCode("CUST");

        Guid customerId;
        string customerSlug;
        using (var customerScope = _factory.Services.CreateScope())
        {
            var bll = customerScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var customer = await bll.Customers.CreateAndGetProfileAsync(
                CompanyRoute(),
                NewCustomerDto(customerName, registryCode));

            customer.IsSuccess.Should().BeTrue();
            customerId = customer.Value.Id;
            customerSlug = customer.Value.Slug;
        }

        Guid propertyId;
        string propertySlug;
        using (var propertyScope = _factory.Services.CreateScope())
        {
            var bll = propertyScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var property = await bll.Properties.CreateAndGetProfileAsync(
                CustomerRoute(customerSlug),
                new PropertyBllDto
                {
                    PropertyTypeId = TestTenants.PropertyTypeReferencedId,
                    Label = propertyName,
                    AddressLine = "Workflow Street 1",
                    City = "Tallinn",
                    PostalCode = "10111",
                    Notes = "Workflow property"
                });

            property.IsSuccess.Should().BeTrue();
            propertyId = property.Value.PropertyId;
            propertySlug = property.Value.PropertySlug;
        }

        Guid unitId;
        string unitSlug;
        using (var unitScope = _factory.Services.CreateScope())
        {
            var bll = unitScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var unit = await bll.Units.CreateAndGetProfileAsync(
                PropertyRoute(customerSlug, propertySlug),
                new UnitBllDto
                {
                    UnitNr = unitNr,
                    FloorNr = 1,
                    SizeM2 = 48.5m,
                    Notes = "Workflow unit"
                });

            unit.IsSuccess.Should().BeTrue();
            unitId = unit.Value.UnitId;
            unitSlug = unit.Value.UnitSlug;
        }

        return new Hierarchy(
            customerId,
            customerSlug,
            customerName,
            registryCode,
            propertyId,
            propertySlug,
            propertyName,
            unitId,
            unitSlug,
            unitNr);
    }

    private async Task<App.BLL.DTO.Customers.Models.CustomerProfileModel> UpdateCustomerAsync(
        string customerSlug,
        string registryCode)
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var updated = await bll.Customers.UpdateAndGetProfileAsync(
            CustomerRoute(customerSlug),
            new CustomerBllDto
            {
                Name = "Updated Workflow Customer",
                RegistryCode = registryCode,
                BillingEmail = "updated-workflow-customer@test.ee",
                BillingAddress = "Updated Customer Street 1",
                Phone = "+372 5555 7001"
            });

        updated.IsSuccess.Should().BeTrue();
        return updated.Value;
    }

    private async Task<App.BLL.DTO.Properties.Models.PropertyProfileModel> UpdatePropertyAsync(
        string customerSlug,
        string propertySlug)
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var updated = await bll.Properties.UpdateAndGetProfileAsync(
            PropertyRoute(customerSlug, propertySlug),
            new PropertyBllDto
            {
                Label = "Updated Workflow Property",
                AddressLine = "Updated Property Street 1",
                City = "Tartu",
                PostalCode = "50111",
                Notes = "Updated workflow property"
            });

        updated.IsSuccess.Should().BeTrue();
        return updated.Value;
    }

    private async Task<App.BLL.DTO.Units.Models.UnitProfileModel> UpdateUnitAsync(
        string customerSlug,
        string propertySlug,
        string unitSlug)
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var updated = await bll.Units.UpdateAndGetProfileAsync(
            UnitRoute(customerSlug, propertySlug, unitSlug),
            new UnitBllDto
            {
                UnitNr = "WF-201",
                FloorNr = 2,
                SizeM2 = 62.5m,
                Notes = "Updated workflow unit"
            });

        updated.IsSuccess.Should().BeTrue();
        return updated.Value;
    }

    private static ManagementCompanyRoute CompanyRoute()
    {
        return new ManagementCompanyRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug
        };
    }

    private static CustomerRoute CustomerRoute(string customerSlug)
    {
        return new CustomerRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            CustomerSlug = customerSlug
        };
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

    private static CustomerBllDto NewCustomerDto(string name, string registryCode)
    {
        return new CustomerBllDto
        {
            Name = name,
            RegistryCode = registryCode,
            BillingEmail = $"{registryCode.ToLowerInvariant()}@test.ee",
            BillingAddress = "Workflow Customer Street 1",
            Phone = "+372 5555 7000"
        };
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }

    private sealed record Hierarchy(
        Guid CustomerId,
        string CustomerSlug,
        string CustomerName,
        string CustomerRegistryCode,
        Guid PropertyId,
        string PropertySlug,
        string PropertyName,
        Guid UnitId,
        string UnitSlug,
        string UnitNr);
}
