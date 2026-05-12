using System.Net;
using System.Net.Http.Json;
using App.DTO.v1.Workspace;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.API;

public class WorkspacesController_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WorkspacesController_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CatalogRequiresJwtAndReturnsAccessibleManagementContext()
    {
        using var anonymous = _factory.CreateClientNoRedirect();
        using var authorized = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);

        var unauthorized = await anonymous.GetAsync("/api/v1/workspaces");
        var response = await authorized.GetAsync("/api/v1/workspaces");
        var catalog = await response.Content.ReadFromJsonAsync<WorkspaceCatalogDto>();

        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        catalog.Should().NotBeNull();
        catalog!.ManagementCompanies.Should().ContainSingle(option =>
            option.Id == TestTenants.CompanyAId
            && option.ContextType == "management"
            && option.ManagementCompanySlug == TestTenants.CompanyASlug
            && option.Path == $"/companies/{TestTenants.CompanyASlug}");
        catalog.DefaultContext.Should().NotBeNull();
        catalog.DefaultContext!.Id.Should().Be(TestTenants.CompanyAId);
    }

    [Fact]
    public async Task DefaultRedirectAndSelectReturnManagementWorkspacePath()
    {
        using var client = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);

        var defaultRedirect = await client.GetAsync("/api/v1/workspaces/default-redirect");
        var defaultDto = await defaultRedirect.Content.ReadFromJsonAsync<WorkspaceRedirectDto>();
        var selected = await client.PostAsJsonAsync("/api/v1/workspaces/select", new SelectWorkspaceDto
        {
            ContextType = "management",
            ContextId = TestTenants.CompanyAId
        });
        var selectedDto = await selected.Content.ReadFromJsonAsync<WorkspaceRedirectDto>();

        defaultRedirect.StatusCode.Should().Be(HttpStatusCode.OK);
        defaultDto.Should().NotBeNull();
        defaultDto!.Destination.Should().Be("ManagementDashboard");
        defaultDto.Path.Should().Be($"/companies/{TestTenants.CompanyASlug}");
        selected.StatusCode.Should().Be(HttpStatusCode.OK);
        selectedDto.Should().NotBeNull();
        selectedDto!.Destination.Should().Be("management");
        selectedDto.CompanySlug.Should().Be(TestTenants.CompanyASlug);
        selectedDto.Path.Should().Be($"/companies/{TestTenants.CompanyASlug}");
    }

    [Fact]
    public async Task SelectRejectsMissingOrUnauthorizedContext()
    {
        using var client = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);

        var missingContext = await client.PostAsJsonAsync("/api/v1/workspaces/select", new SelectWorkspaceDto
        {
            ContextType = "management",
            ContextId = Guid.Empty
        });
        var unauthorizedContext = await client.PostAsJsonAsync("/api/v1/workspaces/select", new SelectWorkspaceDto
        {
            ContextType = "management",
            ContextId = Guid.NewGuid()
        });

        missingContext.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        unauthorizedContext.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
