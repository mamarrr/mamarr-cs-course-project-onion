using System.Net;
using System.Net.Http.Json;
using App.DTO.v1.Common;
using App.DTO.v1.Identity;
using App.DTO.v1.Portal.Users;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.API;

public class CompanyUsersApi_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CompanyUsersApi_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompanyUsersApi_RequiresJwtAndCompanyAdminAccess()
    {
        using var anonymous = _factory.CreateClientNoRedirect();
        using var owner = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);
        using var systemAdmin = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.SystemAdmin);

        var unauthorized = await anonymous.GetAsync(CompanyUsersPath());
        var forbidden = await systemAdmin.GetAsync(CompanyUsersPath());
        var response = await owner.GetAsync(CompanyUsersPath());
        var page = await response.Content.ReadFromJsonAsync<CompanyUsersPageDto>();

        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        page.Should().NotBeNull();
        page!.ManagementCompanyId.Should().Be(TestTenants.CompanyAId);
        page.CurrentActorIsOwner.Should().BeTrue();
        page.Members.Should().Contain(member =>
            member.AppUserId == TestUsers.CompanyAOwnerId
            && member.Email == TestUsers.CompanyAOwnerEmail
            && member.RoleCode == "OWNER");
        page.Roles.Should().Contain(role => role.RoleCode == "MANAGER");
    }

    [Fact]
    public async Task CompanyUsersApi_AddUpdateDeleteMembership_Workflow()
    {
        using var publicClient = _factory.CreateClientNoRedirect();
        using var owner = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);
        var email = UniqueEmail("company-user");
        var roles = await owner.GetFromJsonAsync<List<CompanyUserRoleOptionDto>>(CompanyRolesPath());
        var managerRole = roles!.Single(role => role.RoleCode == "MANAGER");
        var supportRole = roles!.Single(role => role.RoleCode == "SUPPORT");

        var register = await publicClient.PostAsJsonAsync("/api/v1/auth/register", new RegisterInfo
        {
            Email = email,
            Password = TestUsers.Password,
            FirstName = "Company",
            LastName = "Member"
        });
        var invalidAdd = await owner.PostAsJsonAsync(CompanyUsersPath(), new AddCompanyUserDto
        {
            Email = "",
            RoleId = managerRole.RoleId,
            JobTitle = "Invalid member",
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date)
        });
        var added = await owner.PostAsJsonAsync(CompanyUsersPath(), new AddCompanyUserDto
        {
            Email = email,
            RoleId = managerRole.RoleId,
            JobTitle = "Property manager",
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date)
        });
        var addedMember = await added.Content.ReadFromJsonAsync<CompanyUserEditDto>();
        var get = await owner.GetAsync(CompanyUserPath(addedMember!.MembershipId));
        var fetchedMember = await get.Content.ReadFromJsonAsync<CompanyUserEditDto>();
        var updated = await owner.PutAsJsonAsync(CompanyUserPath(addedMember.MembershipId), new UpdateCompanyUserDto
        {
            RoleId = supportRole.RoleId,
            JobTitle = "Support specialist",
            ValidFrom = addedMember.ValidFrom,
            ValidTo = null
        });
        var updatedMember = await updated.Content.ReadFromJsonAsync<CompanyUserEditDto>();
        var deleted = await owner.DeleteAsync(CompanyUserPath(addedMember.MembershipId));
        var deleteResult = await deleted.Content.ReadFromJsonAsync<CommandResultDto>();
        var afterDelete = await owner.GetAsync(CompanyUserPath(addedMember.MembershipId));

        register.StatusCode.Should().Be(HttpStatusCode.Created);
        invalidAdd.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        added.StatusCode.Should().Be(HttpStatusCode.Created);
        added.Headers.Location.Should().NotBeNull();
        addedMember.Email.Should().Be(email);
        addedMember.RoleCode.Should().Be("MANAGER");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        fetchedMember.Should().NotBeNull();
        fetchedMember!.MembershipId.Should().Be(addedMember.MembershipId);
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedMember.Should().NotBeNull();
        updatedMember!.RoleCode.Should().Be("SUPPORT");
        updatedMember.JobTitle.Should().Be("Support specialist");
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);
        deleteResult.Should().NotBeNull();
        deleteResult!.Success.Should().BeTrue();
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static string CompanyUsersPath()
    {
        return $"/api/v1/portal/companies/{TestTenants.CompanyASlug}/users";
    }

    private static string CompanyRolesPath()
    {
        return $"{CompanyUsersPath()}/roles";
    }

    private static string CompanyUserPath(Guid membershipId)
    {
        return $"{CompanyUsersPath()}/{membershipId:D}";
    }

    private static string UniqueEmail(string prefix)
    {
        return $"api-{prefix}-{Guid.NewGuid():N}@test.ee";
    }
}
