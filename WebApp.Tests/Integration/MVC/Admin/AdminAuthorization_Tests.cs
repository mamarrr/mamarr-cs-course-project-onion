using System.Net;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.MVC.Admin;

public class AdminAuthorization_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminAuthorization_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anonymous_AdminDashboard_RedirectsToLogin()
    {
        using var client = _factory.CreateClientNoRedirect();

        var response = await client.GetAsync("/Admin");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("Login");
    }

    [Fact]
    public async Task NonAdmin_AdminDashboard_IsForbidden()
    {
        using var client = _factory.CreateAuthenticatedMvcClient(TestUsers.CompanyAOwner);

        var response = await client.GetAsync("/Admin");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SystemAdmin_AdminDashboard_Loads()
    {
        using var client = _factory.CreateAuthenticatedMvcClient(TestUsers.SystemAdmin);

        var response = await client.GetAsync("/Admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NonAdmin_AdminDetailsPage_IsForbidden()
    {
        using var client = _factory.CreateAuthenticatedMvcClient(TestUsers.CompanyAOwner);

        var response = await client.GetAsync($"/Admin/Users/{TestUsers.CompanyAOwnerId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminPost_WithoutAntiForgeryToken_IsRejected()
    {
        using var client = _factory.CreateAuthenticatedMvcClient(TestUsers.SystemAdmin);

        var response = await client.PostAsync(
            $"/Admin/Users/{TestUsers.CompanyAOwnerId}/lock",
            new FormUrlEncodedContent([]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
