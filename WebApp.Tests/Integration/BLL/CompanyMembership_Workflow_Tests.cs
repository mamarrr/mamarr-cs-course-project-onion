using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.ManagementCompanies.Commands;
using App.DAL.EF;
using App.Domain.Identity;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.BLL;

public class CompanyMembership_Workflow_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CompanyMembership_Workflow_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OwnerAuthorizesForCompanyAdministrationAndListsSeededMember()
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var auth = await bll.CompanyMemberships.AuthorizeAsync(new ManagementCompanyRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug
        });
        var members = await bll.CompanyMemberships.ListCompanyMembersAsync(auth.Value);

        auth.IsSuccess.Should().BeTrue();
        auth.Value.IsOwner.Should().BeTrue();
        auth.Value.IsAdmin.Should().BeTrue();
        auth.Value.ActorRoleCode.Should().Be("OWNER");
        members.Value.Members.Should().Contain(member => member.AppUserId == TestUsers.CompanyAOwnerId && member.RoleCode == "OWNER");
    }

    [Fact]
    public async Task NonMemberCannotAuthorizeForCompanyAdministration()
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var auth = await bll.CompanyMemberships.AuthorizeAsync(new ManagementCompanyRoute
        {
            AppUserId = TestUsers.SystemAdminId,
            CompanySlug = TestTenants.CompanyASlug
        });

        auth.IsFailed.Should().BeTrue();
        auth.Errors.Should().Contain(error => error is ForbiddenError);
    }

    [Fact]
    public async Task UserSubmitsJoinRequest_DuplicateFails_AndOwnerApprovesMembership()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var user = await CreateUserAsync(db, "join-approve");
        var roleId = await RoleIdAsync(db, "SUPPORT");

        var create = await bll.CompanyMemberships.CreateJoinRequestAsync(new CreateCompanyJoinRequestCommand
        {
            AppUserId = user.Id,
            RegistryCode = " TEST-COMPANY-A ",
            RequestedRoleId = roleId,
            Message = "Please approve"
        });
        var duplicate = await bll.CompanyMemberships.CreateJoinRequestAsync(new CreateCompanyJoinRequestCommand
        {
            AppUserId = user.Id,
            RegistryCode = "TEST-COMPANY-A",
            RequestedRoleId = roleId,
            Message = "Please approve twice"
        });
        var ownerContext = (await bll.CompanyMemberships.AuthorizeAsync(new ManagementCompanyRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug
        })).Value;
        var pending = await bll.CompanyMemberships.GetPendingAccessRequestsAsync(ownerContext);
        var request = pending.Value.Requests.Single(item => item.AppUserId == user.Id);
        var approved = await bll.CompanyMemberships.ApprovePendingAccessRequestAsync(ownerContext, request.RequestId);

        create.IsSuccess.Should().BeTrue();
        duplicate.IsFailed.Should().BeTrue();
        duplicate.Errors.Should().Contain(error => error is ConflictError);
        approved.IsSuccess.Should().BeTrue();

        var joinedRole = await db.ManagementCompanyUsers
            .AsNoTracking()
            .Where(membership => membership.ManagementCompanyId == TestTenants.CompanyAId && membership.AppUserId == user.Id)
            .Select(membership => membership.ManagementCompanyRole!.Code)
            .SingleAsync();
        joinedRole.Should().Be("SUPPORT");
    }

    [Fact]
    public async Task OwnerRejectsJoinRequest_AndNoMembershipIsCreated()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var user = await CreateUserAsync(db, "join-reject");
        var roleId = await RoleIdAsync(db, "FINANCE");

        var create = await bll.CompanyMemberships.CreateJoinRequestAsync(new CreateCompanyJoinRequestCommand
        {
            AppUserId = user.Id,
            RegistryCode = "TEST-COMPANY-A",
            RequestedRoleId = roleId,
            Message = "Please reject"
        });
        var ownerContext = (await bll.CompanyMemberships.AuthorizeAsync(new ManagementCompanyRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug
        })).Value;
        var pending = await bll.CompanyMemberships.GetPendingAccessRequestsAsync(ownerContext);
        var request = pending.Value.Requests.Single(item => item.AppUserId == user.Id);
        var rejected = await bll.CompanyMemberships.RejectPendingAccessRequestAsync(ownerContext, request.RequestId);

        create.IsSuccess.Should().BeTrue();
        rejected.IsSuccess.Should().BeTrue();
        var membershipExists = await db.ManagementCompanyUsers
            .AsNoTracking()
            .AnyAsync(membership => membership.ManagementCompanyId == TestTenants.CompanyAId && membership.AppUserId == user.Id);
        membershipExists.Should().BeFalse();
    }

    [Fact]
    public async Task CreateJoinRequest_RejectsExistingMemberMissingCompanyAndInvalidRole()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var user = await CreateUserAsync(db, "join-invalid");
        var roleId = await RoleIdAsync(db, "SUPPORT");

        var existingMember = await bll.CompanyMemberships.CreateJoinRequestAsync(new CreateCompanyJoinRequestCommand
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            RegistryCode = "TEST-COMPANY-A",
            RequestedRoleId = roleId
        });
        var missingCompany = await bll.CompanyMemberships.CreateJoinRequestAsync(new CreateCompanyJoinRequestCommand
        {
            AppUserId = user.Id,
            RegistryCode = "MISSING-COMPANY",
            RequestedRoleId = roleId
        });
        var invalidRole = await bll.CompanyMemberships.CreateJoinRequestAsync(new CreateCompanyJoinRequestCommand
        {
            AppUserId = user.Id,
            RegistryCode = "TEST-COMPANY-A",
            RequestedRoleId = Guid.NewGuid()
        });

        existingMember.IsFailed.Should().BeTrue();
        existingMember.Errors.Should().Contain(error => error is ConflictError);
        missingCompany.IsFailed.Should().BeTrue();
        missingCompany.Errors.Should().Contain(error => error is NotFoundError);
        invalidRole.IsFailed.Should().BeTrue();
        invalidRole.Errors.Should().Contain(error => error is ValidationAppError);
    }

    private static async Task<AppUser> CreateUserAsync(AppDbContext db, string suffix)
    {
        var unique = Guid.NewGuid().ToString("N");
        var email = $"membership-{suffix}-{unique}@test.ee";
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = "Membership",
            LastName = suffix,
            CreatedAt = DateTime.UtcNow
        };

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Guid> RoleIdAsync(AppDbContext db, string code)
    {
        return await db.ManagementCompanyRoles
            .AsNoTracking()
            .Where(role => role.Code == code)
            .Select(role => role.Id)
            .SingleAsync();
    }
}
