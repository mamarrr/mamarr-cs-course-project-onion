using System.Net;
using System.Net.Http.Json;
using App.DTO.v1.Portal.Leases;
using App.DTO.v1.Portal.Tickets;
using App.DTO.v1.Shared;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.API;

public class LookupApi_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LookupApi_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GlobalLookupEndpointsRequireJwtAndReturnSeededOptions()
    {
        using var anonymous = _factory.CreateClientNoRedirect();
        using var owner = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);

        var unauthorized = await anonymous.GetAsync(PropertyTypesPath());
        var propertyTypes = await owner.GetAsync(PropertyTypesPath());
        var propertyTypeOptions = await propertyTypes.Content.ReadFromJsonAsync<List<LookupOptionDto>>();
        var leaseRoles = await owner.GetAsync(LeaseRolesPath());
        var leaseRoleOptions = await leaseRoles.Content.ReadFromJsonAsync<LeaseRoleOptionsDto>();

        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        propertyTypes.StatusCode.Should().Be(HttpStatusCode.OK);
        propertyTypeOptions.Should().NotBeNull();
        propertyTypeOptions.Should().Contain(option => option.Id == TestTenants.PropertyTypeReferencedId);
        leaseRoles.StatusCode.Should().Be(HttpStatusCode.OK);
        leaseRoleOptions.Should().NotBeNull();
        leaseRoleOptions!.Roles.Should().NotBeEmpty();
        leaseRoleOptions.Roles.Should().OnlyContain(role => role.LeaseRoleId != Guid.Empty);
    }

    [Fact]
    public async Task TicketOptionsAreTenantScopedAndCanBeFilteredByParentContext()
    {
        using var anonymous = _factory.CreateClientNoRedirect();
        using var owner = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);

        var unauthorized = await anonymous.GetAsync(TicketOptionsPath());
        var options = await owner.GetAsync(TicketOptionsPath());
        var optionDto = await options.Content.ReadFromJsonAsync<TicketSelectorOptionsDto>();
        var filtered = await owner.GetAsync($"{TicketOptionsPath()}?CustomerId={TestTenants.CustomerAId:D}&PropertyId={TestTenants.PropertyAId:D}");
        var filteredDto = await filtered.Content.ReadFromJsonAsync<TicketSelectorOptionsDto>();
        var missingCompany = await owner.GetAsync($"/api/v1/portal/companies/missing-company/lookups/ticket-options");

        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        options.StatusCode.Should().Be(HttpStatusCode.OK);
        optionDto.Should().NotBeNull();
        optionDto!.Statuses.Should().Contain(option => option.Id == TestTenants.TicketStatusCreatedId);
        optionDto.Priorities.Should().Contain(option => option.Id == TestTenants.TicketPriorityReferencedId);
        optionDto.Categories.Should().Contain(option => option.Id == TestTenants.TicketCategoryReferencedId);
        optionDto.Customers.Should().Contain(option => option.Id == TestTenants.CustomerAId);
        optionDto.Properties.Should().Contain(option => option.Id == TestTenants.PropertyAId);
        optionDto.Units.Should().Contain(option => option.Id == TestTenants.UnitAId);
        optionDto.Vendors.Should().Contain(option => option.Id == TestTenants.VendorAId);

        filtered.StatusCode.Should().Be(HttpStatusCode.OK);
        filteredDto.Should().NotBeNull();
        filteredDto!.Properties.Should().Contain(option => option.Id == TestTenants.PropertyAId);
        filteredDto.Units.Should().Contain(option => option.Id == TestTenants.UnitAId);
        missingCompany.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static string PropertyTypesPath()
    {
        return "/api/v1/portal/lookups/property-types";
    }

    private static string LeaseRolesPath()
    {
        return "/api/v1/portal/lookups/lease-roles";
    }

    private static string TicketOptionsPath()
    {
        return $"/api/v1/portal/companies/{TestTenants.CompanyASlug}/lookups/ticket-options";
    }
}
