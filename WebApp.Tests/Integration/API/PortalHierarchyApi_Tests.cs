using System.Net;
using System.Net.Http.Json;
using App.DTO.v1.Common;
using App.DTO.v1.Portal.Companies;
using App.DTO.v1.Portal.Customers;
using App.DTO.v1.Portal.Dashboards;
using App.DTO.v1.Portal.Properties;
using App.DTO.v1.Portal.Units;
using App.DTO.v1.Shared;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.API;

public class PortalHierarchyApi_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PortalHierarchyApi_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompanyProfileRequiresJwtLoadsAndRejectsUnauthorizedTenant()
    {
        using var anonymous = _factory.CreateClientNoRedirect();
        using var owner = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);
        using var systemAdmin = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.SystemAdmin);

        var unauthorized = await anonymous.GetAsync(CompanyPath());
        var forbidden = await systemAdmin.GetAsync(CompanyPath());
        var profileResponse = await owner.GetAsync(CompanyPath());
        var profile = await profileResponse.Content.ReadFromJsonAsync<ManagementCompanyProfileDto>();

        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        profile.Should().NotBeNull();
        profile!.ManagementCompanyId.Should().Be(TestTenants.CompanyAId);
        profile.CompanySlug.Should().Be(TestTenants.CompanyASlug);
    }

    [Fact]
    public async Task DashboardApi_LoadsManagementCustomerPropertyAndUnitDashboards()
    {
        using var client = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);

        var management = await client.GetAsync(DashboardPath());
        var managementDashboard = await management.Content.ReadFromJsonAsync<ManagementDashboardDto>();
        var customer = await client.GetAsync(CustomerDashboardPath("customer-a"));
        var customerDashboard = await customer.Content.ReadFromJsonAsync<CustomerDashboardDto>();
        var property = await client.GetAsync(PropertyDashboardPath("customer-a", "property-a"));
        var propertyDashboard = await property.Content.ReadFromJsonAsync<PropertyDashboardDto>();
        var unit = await client.GetAsync(UnitDashboardPath("customer-a", "property-a", "a-101"));
        var unitDashboard = await unit.Content.ReadFromJsonAsync<UnitDashboardDto>();
        var missingCustomer = await client.GetAsync(CustomerDashboardPath("missing-customer"));

        management.StatusCode.Should().Be(HttpStatusCode.OK);
        managementDashboard.Should().NotBeNull();
        managementDashboard!.Context.ManagementCompanyId.Should().Be(TestTenants.CompanyAId);
        managementDashboard.Context.CompanySlug.Should().Be(TestTenants.CompanyASlug);

        customer.StatusCode.Should().Be(HttpStatusCode.OK);
        customerDashboard.Should().NotBeNull();
        customerDashboard!.Context.CustomerId.Should().Be(TestTenants.CustomerAId);
        customerDashboard.Context.CustomerSlug.Should().Be("customer-a");

        property.StatusCode.Should().Be(HttpStatusCode.OK);
        propertyDashboard.Should().NotBeNull();
        propertyDashboard!.Context.PropertyId.Should().Be(TestTenants.PropertyAId);
        propertyDashboard.Context.PropertySlug.Should().Be("property-a");

        unit.StatusCode.Should().Be(HttpStatusCode.OK);
        unitDashboard.Should().NotBeNull();
        unitDashboard!.Context.UnitId.Should().Be(TestTenants.UnitAId);
        unitDashboard.Context.UnitSlug.Should().Be("a-101");
        missingCustomer.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CustomerApi_CreateListUpdateDelete_Workflow()
    {
        using var client = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);
        var registryCode = UniqueCode("CUST");

        var invalid = await client.PostAsJsonAsync(CustomersPath(), new CustomerRequestDto
        {
            Name = "",
            RegistryCode = registryCode,
            BillingEmail = "invalid-customer@test.ee",
            BillingAddress = "Invalid Street 1",
            Phone = "+372 5555 9201"
        });
        var created = await client.PostAsJsonAsync(CustomersPath(), new CustomerRequestDto
        {
            Name = "API Customer",
            RegistryCode = registryCode,
            BillingEmail = "api-customer@test.ee",
            BillingAddress = "API Customer Street 1",
            Phone = "+372 5555 9202"
        });
        var createdProfile = await created.Content.ReadFromJsonAsync<CustomerProfileDto>();
        var list = await client.GetFromJsonAsync<List<CustomerListItemDto>>(CustomersPath());
        var updated = await client.PutAsJsonAsync(CustomerProfilePath(createdProfile!.Slug), new CustomerRequestDto
        {
            Name = "API Customer Updated",
            RegistryCode = registryCode,
            BillingEmail = "api-customer-updated@test.ee",
            BillingAddress = "API Customer Street 2",
            Phone = "+372 5555 9203"
        });
        var updatedProfile = await updated.Content.ReadFromJsonAsync<CustomerProfileDto>();
        var deleted = await client.SendAsync(JsonRequest(
            HttpMethod.Delete,
            CustomerProfilePath(updatedProfile!.Slug),
            new DeleteConfirmationDto { DeleteConfirmation = updatedProfile.Name }));
        var afterDelete = await client.GetAsync(CustomerProfilePath(updatedProfile.Slug));

        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.Location.Should().NotBeNull();
        createdProfile.Name.Should().Be("API Customer");
        list.Should().Contain(customer => customer.CustomerSlug == createdProfile.Slug);
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedProfile.Name.Should().Be("API Customer Updated");
        updatedProfile.BillingEmail.Should().Be("api-customer-updated@test.ee");
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PropertyApi_CreateListUpdateDeleteAndRejectWrongParent_Workflow()
    {
        using var client = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);
        var customer = await CreateCustomerAsync(client, "property-parent");

        var wrongParent = await client.GetAsync(PropertiesPath("missing-customer"));
        var invalid = await client.PostAsJsonAsync(PropertiesPath(customer.Slug), new CreatePropertyDto
        {
            Name = "",
            PropertyTypeId = TestTenants.PropertyTypeReferencedId,
            AddressLine = "Invalid Property Street 1",
            City = "Tallinn",
            PostalCode = "10111"
        });
        var created = await client.PostAsJsonAsync(PropertiesPath(customer.Slug), new CreatePropertyDto
        {
            Name = "API Property",
            PropertyTypeId = TestTenants.PropertyTypeReferencedId,
            AddressLine = "API Property Street 1",
            City = "Tallinn",
            PostalCode = "10112",
            Notes = "Property notes"
        });
        var createdProfile = await created.Content.ReadFromJsonAsync<PropertyProfileDto>();
        var list = await client.GetFromJsonAsync<List<PropertyListItemDto>>(PropertiesPath(customer.Slug));
        var updated = await client.PutAsJsonAsync(PropertyProfilePath(customer.Slug, createdProfile!.PropertySlug), new UpdatePropertyProfileDto
        {
            Name = "API Property Updated",
            AddressLine = "API Property Street 2",
            City = "Tartu",
            PostalCode = "50101",
            Notes = "Updated property notes"
        });
        var updatedProfile = await updated.Content.ReadFromJsonAsync<PropertyProfileDto>();
        var deleted = await client.SendAsync(JsonRequest(
            HttpMethod.Delete,
            PropertyProfilePath(customer.Slug, updatedProfile!.PropertySlug),
            new DeleteConfirmationDto { DeleteConfirmation = updatedProfile.Name }));
        var afterDelete = await client.GetAsync(PropertyProfilePath(customer.Slug, updatedProfile.PropertySlug));

        wrongParent.StatusCode.Should().Be(HttpStatusCode.NotFound);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        createdProfile.Name.Should().Be("API Property");
        list.Should().Contain(property => property.PropertySlug == createdProfile.PropertySlug);
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedProfile.Name.Should().Be("API Property Updated");
        updatedProfile.City.Should().Be("Tartu");
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnitApi_CreateListUpdateDelete_Workflow()
    {
        using var client = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);
        var customer = await CreateCustomerAsync(client, "unit-parent");
        var property = await CreatePropertyAsync(client, customer.Slug, "unit-parent");

        var invalid = await client.PostAsJsonAsync(UnitsPath(customer.Slug, property.PropertySlug), new UnitRequestDto
        {
            UnitNr = ""
        });
        var created = await client.PostAsJsonAsync(UnitsPath(customer.Slug, property.PropertySlug), new UnitRequestDto
        {
            UnitNr = $"API-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            FloorNr = 3,
            SizeM2 = 56.5m,
            Notes = "Unit notes"
        });
        var createdProfile = await created.Content.ReadFromJsonAsync<UnitProfileDto>();
        var list = await client.GetFromJsonAsync<List<UnitListItemDto>>(UnitsPath(customer.Slug, property.PropertySlug));
        var updated = await client.PutAsJsonAsync(UnitProfilePath(customer.Slug, property.PropertySlug, createdProfile!.UnitSlug), new UnitRequestDto
        {
            UnitNr = createdProfile.UnitNr,
            FloorNr = 4,
            SizeM2 = 60.25m,
            Notes = "Updated unit notes"
        });
        var updatedProfile = await updated.Content.ReadFromJsonAsync<UnitProfileDto>();
        var deleted = await client.SendAsync(JsonRequest(
            HttpMethod.Delete,
            UnitProfilePath(customer.Slug, property.PropertySlug, updatedProfile!.UnitSlug),
            new DeleteConfirmationDto { DeleteConfirmation = updatedProfile.UnitNr }));
        var deleteResult = await deleted.Content.ReadFromJsonAsync<CommandResultDto>();
        var afterDelete = await client.GetAsync(UnitProfilePath(customer.Slug, property.PropertySlug, updatedProfile.UnitSlug));

        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        createdProfile.UnitSlug.Should().NotBeNullOrWhiteSpace();
        list.Should().Contain(unit => unit.UnitSlug == createdProfile.UnitSlug);
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedProfile.FloorNr.Should().Be(4);
        updatedProfile.SizeM2.Should().Be(60.25m);
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);
        deleteResult.Should().NotBeNull();
        deleteResult!.Success.Should().BeTrue();
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<CustomerProfileDto> CreateCustomerAsync(HttpClient client, string suffix)
    {
        var created = await client.PostAsJsonAsync(CustomersPath(), new CustomerRequestDto
        {
            Name = $"API Customer {suffix}",
            RegistryCode = UniqueCode("CUST"),
            BillingEmail = $"{suffix}-{Guid.NewGuid():N}@customer.test.ee",
            BillingAddress = "API Customer Helper Street 1",
            Phone = "+372 5555 9301"
        });
        var profile = await created.Content.ReadFromJsonAsync<CustomerProfileDto>();

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        profile.Should().NotBeNull();
        return profile!;
    }

    private async Task<PropertyProfileDto> CreatePropertyAsync(HttpClient client, string customerSlug, string suffix)
    {
        var created = await client.PostAsJsonAsync(PropertiesPath(customerSlug), new CreatePropertyDto
        {
            Name = $"API Property {suffix}",
            PropertyTypeId = TestTenants.PropertyTypeReferencedId,
            AddressLine = "API Property Helper Street 1",
            City = "Tallinn",
            PostalCode = "10113",
            Notes = "Helper property"
        });
        var profile = await created.Content.ReadFromJsonAsync<PropertyProfileDto>();

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        profile.Should().NotBeNull();
        return profile!;
    }

    private static HttpRequestMessage JsonRequest<T>(HttpMethod method, string path, T body)
    {
        return new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body)
        };
    }

    private static string CompanyPath()
    {
        return $"/api/v1/portal/companies/{TestTenants.CompanyASlug}";
    }

    private static string DashboardPath()
    {
        return $"{CompanyPath()}/dashboard";
    }

    private static string CustomersPath()
    {
        return $"{CompanyPath()}/customers";
    }

    private static string CustomerProfilePath(string customerSlug)
    {
        return $"{CustomersPath()}/{customerSlug}/profile";
    }

    private static string CustomerDashboardPath(string customerSlug)
    {
        return $"{CustomersPath()}/{customerSlug}/dashboard";
    }

    private static string PropertiesPath(string customerSlug)
    {
        return $"{CustomersPath()}/{customerSlug}/properties";
    }

    private static string PropertyProfilePath(string customerSlug, string propertySlug)
    {
        return $"{PropertiesPath(customerSlug)}/{propertySlug}/profile";
    }

    private static string PropertyDashboardPath(string customerSlug, string propertySlug)
    {
        return $"{PropertiesPath(customerSlug)}/{propertySlug}/dashboard";
    }

    private static string UnitsPath(string customerSlug, string propertySlug)
    {
        return $"{PropertiesPath(customerSlug)}/{propertySlug}/units";
    }

    private static string UnitProfilePath(string customerSlug, string propertySlug, string unitSlug)
    {
        return $"{UnitsPath(customerSlug, propertySlug)}/{unitSlug}/profile";
    }

    private static string UnitDashboardPath(string customerSlug, string propertySlug, string unitSlug)
    {
        return $"{UnitsPath(customerSlug, propertySlug)}/{unitSlug}/dashboard";
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }
}
