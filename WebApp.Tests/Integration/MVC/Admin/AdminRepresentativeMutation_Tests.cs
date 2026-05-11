using System.Net;
using App.DAL.EF;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.MVC.Admin;

public class AdminRepresentativeMutation_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminRepresentativeMutation_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_CanLockAndUnlockNormalUser()
    {
        using var client = _factory.CreateAuthenticatedMvcClient(TestUsers.SystemAdmin);

        var lockToken = await TokenFromAsync(client, $"/Admin/Users/{TestUsers.CompanyAOwnerId}");
        var lockResponse = await client.PostAsync(
            $"/Admin/Users/{TestUsers.CompanyAOwnerId}/lock",
            FormWithToken(lockToken));

        lockResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await IsLockedAsync(TestUsers.CompanyAOwnerId)).Should().BeTrue();

        var unlockToken = await TokenFromAsync(client, $"/Admin/Users/{TestUsers.CompanyAOwnerId}");
        var unlockResponse = await client.PostAsync(
            $"/Admin/Users/{TestUsers.CompanyAOwnerId}/unlock",
            FormWithToken(unlockToken));

        unlockResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await IsLockedAsync(TestUsers.CompanyAOwnerId)).Should().BeFalse();
    }

    [Fact]
    public async Task Admin_CannotLockSelf()
    {
        using var client = _factory.CreateAuthenticatedMvcClient(TestUsers.SystemAdmin);

        var token = await TokenFromAsync(client, $"/Admin/Users/{TestUsers.SystemAdminId}");
        var response = await client.PostAsync(
            $"/Admin/Users/{TestUsers.SystemAdminId}/lock",
            FormWithToken(token));
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("cannot lock their own account");
        (await IsLockedAsync(TestUsers.SystemAdminId)).Should().BeFalse();
    }

    [Fact]
    public async Task Admin_CanEditManagementCompanyAllowedFields()
    {
        using var client = _factory.CreateAuthenticatedMvcClient(TestUsers.SystemAdmin);
        var token = await TokenFromAsync(client, $"/Admin/Companies/{TestTenants.CompanyAId}/edit");

        var response = await client.PostAsync(
            $"/Admin/Companies/{TestTenants.CompanyAId}/edit",
            FormWithToken(token, new Dictionary<string, string>
            {
                ["Id"] = TestTenants.CompanyAId.ToString(),
                ["Name"] = "Company A Maintenance Updated",
                ["RegistryCode"] = "TEST-COMPANY-A",
                ["VatNumber"] = "EE100000001",
                ["Email"] = "company-a-updated@test.ee",
                ["Phone"] = "+372 5555 0002",
                ["Address"] = "Admin Test Street 2",
                ["Slug"] = TestTenants.CompanyASlug
            }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var company = await db.ManagementCompanies.AsNoTracking().SingleAsync(company => company.Id == TestTenants.CompanyAId);
        company.Name.Should().Be("Company A Maintenance Updated");
        company.Email.Should().Be("company-a-updated@test.ee");
    }

    [Fact]
    public async Task Admin_CanCreateAndDeleteSafeLookupItem()
    {
        using var client = _factory.CreateAuthenticatedMvcClient(TestUsers.SystemAdmin);
        var code = $"SAFE_{Guid.NewGuid():N}"[..18].ToUpperInvariant();

        var createToken = await TokenFromAsync(client, "/Admin/Lookups/PropertyType/create");
        var createResponse = await client.PostAsync(
            "/Admin/Lookups/PropertyType/create",
            FormWithToken(createToken, new Dictionary<string, string>
            {
                ["Code"] = code,
                ["Label"] = "Safe lookup"
            }));

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdId = await PropertyTypeIdByCodeAsync(code);
        createdId.Should().NotBe(Guid.Empty);

        var deleteToken = await TokenFromAsync(client, $"/Admin/Lookups/PropertyType/{createdId}/delete");
        var deleteResponse = await client.PostAsync(
            $"/Admin/Lookups/PropertyType/{createdId}/delete",
            FormWithToken(deleteToken));

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await PropertyTypeIdByCodeAsync(code)).Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task Admin_CannotDeleteReferencedLookupItem()
    {
        using var client = _factory.CreateAuthenticatedMvcClient(TestUsers.SystemAdmin);
        var token = await TokenFromAsync(client, "/Admin/Lookups/PropertyType/create");

        var response = await client.PostAsync(
            $"/Admin/Lookups/PropertyType/{TestTenants.PropertyTypeReferencedId}/delete",
            FormWithToken(token));
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("cannot be deleted");
        (await PropertyTypeExistsAsync(TestTenants.PropertyTypeReferencedId)).Should().BeTrue();
    }

    private async Task<string> TokenFromAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await AntiforgeryFormHelper.ExtractTokenAsync(response);
    }

    private static FormUrlEncodedContent FormWithToken(string token, Dictionary<string, string>? fields = null)
    {
        var data = fields is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(fields);
        data["__RequestVerificationToken"] = token;
        return new FormUrlEncodedContent(data);
    }

    private async Task<bool> IsLockedAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lockoutEnd = await db.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.LockoutEnd)
            .SingleAsync();
        return lockoutEnd != null && lockoutEnd > DateTimeOffset.UtcNow;
    }

    private async Task<Guid> PropertyTypeIdByCodeAsync(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.PropertyTypes
            .AsNoTracking()
            .Where(propertyType => propertyType.Code == code)
            .Select(propertyType => propertyType.Id)
            .SingleOrDefaultAsync();
    }

    private async Task<bool> PropertyTypeExistsAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.PropertyTypes.AsNoTracking().AnyAsync(propertyType => propertyType.Id == id);
    }
}
