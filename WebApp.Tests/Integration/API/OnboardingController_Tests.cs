using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DAL.EF;
using App.Domain.Identity;
using App.DTO.v1.Onboarding;
using App.DTO.v1.Shared;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.API;

public class OnboardingController_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OnboardingController_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StatusRequiresJwtAndReturnsDefaultManagementContextForMember()
    {
        using var anonymous = _factory.CreateClientNoRedirect();
        using var authorized = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);

        var unauthorized = await anonymous.GetAsync("/api/v1/onboarding/status");
        var status = await authorized.GetAsync("/api/v1/onboarding/status");
        var dto = await status.Content.ReadFromJsonAsync<OnboardingStatusDto>();

        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        status.StatusCode.Should().Be(HttpStatusCode.OK);
        dto.Should().NotBeNull();
        dto!.HasWorkspaceContext.Should().BeTrue();
        dto.DefaultPath.Should().Be($"/companies/{TestTenants.CompanyASlug}");
    }

    [Fact]
    public async Task CreateManagementCompany_ValidatesRequiredFieldsCreatesOwnerAndRejectsDuplicateRegistry()
    {
        using var client = await CreateAuthenticatedApiClientForNewUserAsync("create-company");
        var registryCode = UniqueCode("REG");

        var invalid = await client.PostAsJsonAsync("/api/v1/onboarding/management-companies", new CreateManagementCompanyDto
        {
            Name = " ",
            RegistryCode = registryCode,
            VatNumber = "EE112233441",
            Email = "invalid-onboarding@test.ee",
            Phone = "+372 5555 9101",
            Address = "Invalid Street 1"
        });
        var created = await client.PostAsJsonAsync("/api/v1/onboarding/management-companies", new CreateManagementCompanyDto
        {
            Name = "API Onboarding Company",
            RegistryCode = registryCode,
            VatNumber = "EE112233442",
            Email = "api-onboarding-company@test.ee",
            Phone = "+372 5555 9102",
            Address = "Onboarding Street 1"
        });
        var createdCompany = await created.Content.ReadFromJsonAsync<CreatedManagementCompanyDto>();
        var duplicate = await client.PostAsJsonAsync("/api/v1/onboarding/management-companies", new CreateManagementCompanyDto
        {
            Name = "Duplicate API Company",
            RegistryCode = "TEST-COMPANY-A",
            VatNumber = "EE112233443",
            Email = "duplicate-onboarding-company@test.ee",
            Phone = "+372 5555 9103",
            Address = "Duplicate Street 1"
        });

        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        createdCompany.Should().NotBeNull();
        createdCompany!.Slug.Should().NotBeNullOrWhiteSpace();
        createdCompany.Path.Should().Be($"/companies/{createdCompany.Slug}");
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ManagementCompanyRolesAndJoinRequests_UseBusinessResultStatusCodes()
    {
        using var client = await CreateAuthenticatedApiClientForNewUserAsync("join-request");
        var supportRoleId = await RoleIdAsync("SUPPORT");

        var roles = await client.GetAsync("/api/v1/onboarding/management-company-roles");
        var roleOptions = await roles.Content.ReadFromJsonAsync<List<LookupOptionDto>>();
        var invalidRegistry = await client.PostAsJsonAsync(
            "/api/v1/onboarding/management-company-join-requests",
            new JoinManagementCompanyRequestDto
            {
                RegistryCode = "NO-SUCH-REGISTRY",
                RequestedRoleId = supportRoleId,
                Message = "Invalid registry"
            });
        var invalidRole = await client.PostAsJsonAsync(
            "/api/v1/onboarding/management-company-join-requests",
            new JoinManagementCompanyRequestDto
            {
                RegistryCode = "TEST-COMPANY-A",
                RequestedRoleId = Guid.NewGuid(),
                Message = "Invalid role"
            });
        var valid = await client.PostAsJsonAsync(
            "/api/v1/onboarding/management-company-join-requests",
            new JoinManagementCompanyRequestDto
            {
                RegistryCode = "TEST-COMPANY-A",
                RequestedRoleId = supportRoleId,
                Message = "Please add me"
            });
        var validResult = await valid.Content.ReadFromJsonAsync<JoinRequestResultDto>();
        var duplicate = await client.PostAsJsonAsync(
            "/api/v1/onboarding/management-company-join-requests",
            new JoinManagementCompanyRequestDto
            {
                RegistryCode = "TEST-COMPANY-A",
                RequestedRoleId = supportRoleId,
                Message = "Duplicate"
            });

        roles.StatusCode.Should().Be(HttpStatusCode.OK);
        roleOptions.Should().Contain(role => role.Code == "SUPPORT" && role.Id == supportRoleId);
        invalidRegistry.StatusCode.Should().Be(HttpStatusCode.NotFound);
        invalidRole.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        valid.StatusCode.Should().Be(HttpStatusCode.OK);
        validResult.Should().NotBeNull();
        validResult!.Success.Should().BeTrue();
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<HttpClient> CreateAuthenticatedApiClientForNewUserAsync(string suffix)
    {
        string email;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            email = (await CreateUserAsync(db, suffix)).Email!;
        }

        var token = await JwtTestHelper.GenerateTokenAsync(_factory.Services, email);
        var client = _factory.CreateClientNoRedirect();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<Guid> RoleIdAsync(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.ManagementCompanyRoles
            .AsNoTracking()
            .Where(role => role.Code == code)
            .Select(role => role.Id)
            .SingleAsync();
    }

    private static async Task<AppUser> CreateUserAsync(AppDbContext db, string suffix)
    {
        var unique = Guid.NewGuid().ToString("N");
        var email = $"api-{suffix}-{unique}@test.ee";
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = "Api",
            LastName = suffix,
            CreatedAt = DateTime.UtcNow,
            LockoutEnabled = true,
            SecurityStamp = Guid.NewGuid().ToString("D"),
            ConcurrencyStamp = Guid.NewGuid().ToString("D")
        };

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }
}
