using System.Net;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.MVC.Admin;

public class AdminSmoke_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminSmoke_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public static TheoryData<string> AdminPages => new()
    {
        "/Admin",
        "/Admin/Dashboard",
        "/Admin/Users",
        $"/Admin/Users/{TestUsers.SystemAdminId}",
        "/Admin/Companies",
        $"/Admin/Companies/{TestTenants.CompanyAId}",
        "/Admin/Lookups/PropertyType",
        "/Admin/Tickets",
        $"/Admin/Tickets/{TestTenants.TicketAId}"
    };

    [Theory]
    [MemberData(nameof(AdminPages))]
    public async Task AdminPage_Loads(string path)
    {
        using var client = _factory.CreateAuthenticatedMvcClient(TestUsers.SystemAdmin);

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MissingRepresentativeDetails_ReturnsNotFound()
    {
        using var client = _factory.CreateAuthenticatedMvcClient(TestUsers.SystemAdmin);

        var response = await client.GetAsync($"/Admin/Users/{Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminNavigation_RendersMainSectionLinks()
    {
        using var client = _factory.CreateAuthenticatedMvcClient(TestUsers.SystemAdmin);

        var response = await client.GetAsync("/Admin");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("href=\"/Admin/Dashboard\"");
        html.Should().Contain("href=\"/Admin/Users\"");
        html.Should().Contain("href=\"/Admin/Companies\"");
        html.Should().Contain("href=\"/Admin/Lookups\"");
        html.Should().Contain("href=\"/Admin/Tickets\"");
    }
}
