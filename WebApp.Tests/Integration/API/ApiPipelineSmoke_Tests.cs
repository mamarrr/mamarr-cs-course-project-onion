using System.Net;
using System.Net.Http.Json;
using App.DAL.EF;
using App.DTO.v1;
using App.DTO.v1.Identity;
using App.DTO.v1.Shared;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.API;

public class ApiPipelineSmoke_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ApiPipelineSmoke_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TestHost_CreatesSqliteSchema()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        (await db.Database.CanConnectAsync()).Should().BeTrue();
        (await db.Users.AnyAsync(user => user.Id == TestUsers.SystemAdminId)).Should().BeTrue();
        (await db.ManagementCompanies.AnyAsync(company => company.Id == TestTenants.CompanyAId)).Should().BeTrue();
    }

    [Fact]
    public async Task SwaggerV1_Loads()
    {
        using var client = _factory.CreateClientNoRedirect();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.Should().Contain("\"paths\"");
        json.Should().Contain("/api/v1/auth/me");
    }

    [Fact]
    public async Task UnknownApiRoute_ReturnsNotFound()
    {
        using var client = _factory.CreateClientNoRedirect();

        var response = await client.GetAsync("/api/v1/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnsupportedApiVersion_ReturnsNotFound()
    {
        using var client = _factory.CreateClientNoRedirect();

        var response = await client.GetAsync("/api/v9/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ProtectedApiRoute_WithoutJwt_ReturnsUnauthorized_NotMvcRedirect()
    {
        using var client = _factory.CreateClientNoRedirect();

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task ProtectedApiRoute_WithInvalidJwt_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClientNoRedirect();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-valid-token");

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedApiRoute_WithValidJwt_ReturnsCurrentUser()
    {
        using var client = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.SystemAdmin);

        var response = await client.GetAsync("/api/v1/auth/me");
        var user = await response.Content.ReadFromJsonAsync<UserDto>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        user.Should().NotBeNull();
        user!.Id.Should().Be(TestUsers.SystemAdminId);
        user.Email.Should().Be(TestUsers.SystemAdminEmail);
        user.Roles.Should().Contain("SystemAdmin");
    }

    [Fact]
    public async Task Cors_ForApiRequest_ExposesVersionHeaders()
    {
        using var client = _factory.CreateClientNoRedirect();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Add("Origin", "https://example.test");

        var response = await client.SendAsync(request);

        response.Headers.TryGetValues("Access-Control-Expose-Headers", out var values).Should().BeTrue();
        values!.Should().Contain(value => value.Contains("X-Version", StringComparison.Ordinal));
        values.Should().Contain(value => value.Contains("X-Version-Created-At", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ControllerProducedError_UsesRestApiErrorResponse()
    {
        using var client = _factory.CreateClientNoRedirect();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginInfo
        {
            Email = "",
            Password = ""
        });
        var error = await response.Content.ReadFromJsonAsync<RestApiErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.Should().NotBeNull();
        error!.Status.Should().Be(HttpStatusCode.BadRequest);
        error.ErrorCode.Should().Be(ApiErrorCodes.ValidationFailed);
        error.TraceId.Should().NotBeNullOrWhiteSpace();
    }
}
