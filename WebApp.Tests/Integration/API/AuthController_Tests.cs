using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DTO.v1.Identity;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.API;

public class AuthController_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthController_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ValidatesRequiredFieldsCreatesUserAndRejectsDuplicateEmail()
    {
        using var client = _factory.CreateClientNoRedirect();
        var email = UniqueEmail("register");

        var missing = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterInfo
        {
            Email = "",
            Password = TestUsers.Password,
            FirstName = "Api",
            LastName = "Register"
        });
        var created = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterInfo
        {
            Email = email,
            Password = TestUsers.Password,
            FirstName = "Api",
            LastName = "Register"
        });
        var createdUser = await created.Content.ReadFromJsonAsync<UserDto>();
        var duplicate = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterInfo
        {
            Email = email,
            Password = TestUsers.Password,
            FirstName = "Api",
            LastName = "Register"
        });

        missing.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.Location.Should().NotBeNull();
        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be(email);
        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LoginMeRefreshAndLogout_Workflow()
    {
        using var client = _factory.CreateClientNoRedirect();
        var email = UniqueEmail("login");

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterInfo
        {
            Email = email,
            Password = TestUsers.Password,
            FirstName = "Api",
            LastName = "Login"
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        var missingCredentials = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginInfo
        {
            Email = "",
            Password = ""
        });
        var invalidCredentials = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginInfo
        {
            Email = email,
            Password = "Wrong.pass1"
        });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginInfo
        {
            Email = email,
            Password = TestUsers.Password
        });
        var loginTokens = await login.Content.ReadFromJsonAsync<JWTResponse>();

        missingCredentials.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        invalidCredentials.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        loginTokens.Should().NotBeNull();
        loginTokens!.Jwt.Should().NotBeNullOrWhiteSpace();
        loginTokens.RefreshToken.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginTokens.Jwt);
        var me = await client.GetAsync("/api/v1/auth/me");
        var currentUser = await me.Content.ReadFromJsonAsync<UserDto>();

        me.StatusCode.Should().Be(HttpStatusCode.OK);
        currentUser.Should().NotBeNull();
        currentUser!.Email.Should().Be(email);

        var refreshed = await client.PostAsJsonAsync("/api/v1/auth/refresh", new TokenRefreshInfo
        {
            RefreshToken = loginTokens.RefreshToken
        });
        var refreshedTokens = await refreshed.Content.ReadFromJsonAsync<JWTResponse>();
        var oldTokenReuse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new TokenRefreshInfo
        {
            RefreshToken = loginTokens.RefreshToken
        });
        var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutInfo
        {
            RefreshToken = refreshedTokens!.RefreshToken
        });
        var refreshAfterLogout = await client.PostAsJsonAsync("/api/v1/auth/refresh", new TokenRefreshInfo
        {
            RefreshToken = refreshedTokens.RefreshToken
        });

        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        refreshedTokens.Jwt.Should().NotBeNullOrWhiteSpace();
        refreshedTokens.RefreshToken.Should().NotBe(loginTokens.RefreshToken);
        oldTokenReuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        refreshAfterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedMeEndpoint_RejectsMissingAndInvalidJwtWithErrorShape()
    {
        using var client = _factory.CreateClientNoRedirect();

        var missing = await client.GetAsync("/api/v1/auth/me");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-token");
        var invalid = await client.GetAsync("/api/v1/auth/me");

        missing.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        missing.Headers.Location.Should().BeNull();
        invalid.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string UniqueEmail(string prefix)
    {
        return $"api-{prefix}-{Guid.NewGuid():N}@test.ee";
    }
}
