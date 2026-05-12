using App.DAL.Contracts;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.DAL;

public class ManagementCompanyRepository_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ManagementCompanyRepository_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FirstBySlugAsync_ReturnsSeededCompany()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var company = await uow.ManagementCompanies.FirstBySlugAsync($" {TestTenants.CompanyASlug} ");

        company.Should().NotBeNull();
        company!.Id.Should().Be(TestTenants.CompanyAId);
        company.Name.Should().Be(TestTenants.CompanyAName);
        company.RegistryCode.Should().Be("TEST-COMPANY-A");
    }

    [Fact]
    public async Task FirstProfileBySlugAsync_AndFirstProfileByIdAsync_ReturnSameCompanyProfile()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var bySlug = await uow.ManagementCompanies.FirstProfileBySlugAsync(TestTenants.CompanyASlug);
        var byId = await uow.ManagementCompanies.FirstProfileByIdAsync(TestTenants.CompanyAId);

        bySlug.Should().NotBeNull();
        byId.Should().NotBeNull();
        bySlug!.Id.Should().Be(TestTenants.CompanyAId);
        byId!.Slug.Should().Be(TestTenants.CompanyASlug);
        bySlug.Email.Should().Be(byId.Email);
        bySlug.Address.Should().Be(byId.Address);
    }

    [Fact]
    public async Task RegistryCodeExistsAsync_UsesTrimmedRegistryCode()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var exists = await uow.ManagementCompanies.RegistryCodeExistsAsync(" TEST-COMPANY-A ");
        var missing = await uow.ManagementCompanies.RegistryCodeExistsAsync("TEST-COMPANY-MISSING");

        exists.Should().BeTrue();
        missing.Should().BeFalse();
    }

    [Fact]
    public async Task ActiveUserManagementContextsAsync_ReturnsOnlyActiveCompanyMemberships()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var ownerContexts = await uow.ManagementCompanies.ActiveUserManagementContextsAsync(TestUsers.CompanyAOwnerId);
        var adminContexts = await uow.ManagementCompanies.ActiveUserManagementContextsAsync(TestUsers.SystemAdminId);

        ownerContexts.Should().ContainSingle();
        ownerContexts[0].ManagementCompanyId.Should().Be(TestTenants.CompanyAId);
        ownerContexts[0].Slug.Should().Be(TestTenants.CompanyASlug);
        ownerContexts[0].RoleCode.Should().Be("OWNER");
        adminContexts.Should().BeEmpty();
    }

    [Fact]
    public async Task ActiveUserManagementContextExistsBySlugAsync_EnforcesUserMembership()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var ownerHasContext = await uow.ManagementCompanies.ActiveUserManagementContextExistsBySlugAsync(
            TestUsers.CompanyAOwnerId,
            $" {TestTenants.CompanyASlug} ");
        var adminHasContext = await uow.ManagementCompanies.ActiveUserManagementContextExistsBySlugAsync(
            TestUsers.SystemAdminId,
            TestTenants.CompanyASlug);

        ownerHasContext.Should().BeTrue();
        adminHasContext.Should().BeFalse();
    }

    [Fact]
    public async Task MembershipQueries_ReturnRoleAndMembershipState()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var roleCode = await uow.ManagementCompanies.FindActiveUserRoleCodeAsync(
            TestUsers.CompanyAOwnerId,
            TestTenants.CompanyAId);
        var belongsToCompany = await uow.ManagementCompanies.UserBelongsToCompanyAsync(
            TestUsers.CompanyAOwnerId,
            TestTenants.CompanyAId);
        var noContextBelongsToCompany = await uow.ManagementCompanies.UserBelongsToCompanyAsync(
            TestUsers.SystemAdminId,
            TestTenants.CompanyAId);
        var ownerCount = await uow.ManagementCompanies.CountEffectiveOwnersAsync(TestTenants.CompanyAId);

        roleCode.Should().Be("OWNER");
        belongsToCompany.Should().BeTrue();
        noContextBelongsToCompany.Should().BeFalse();
        ownerCount.Should().Be(1);
    }

    [Fact]
    public async Task AllSlugsAsync_ReturnsSeededCompanySlug()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var slugs = await uow.ManagementCompanies.AllSlugsAsync();

        slugs.Should().Contain(TestTenants.CompanyASlug);
    }
}
