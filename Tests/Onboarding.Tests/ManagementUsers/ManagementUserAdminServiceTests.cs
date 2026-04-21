using App.BLL.ManagementCompany.Membership;
using App.BLL.Onboarding;
using App.BLL.Onboarding.CompanyJoinRequests;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using Base.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Onboarding.Tests.ManagementUsers;

public class ManagementUserAdminServiceTests
{
    [Fact]
    public async Task AuthorizeManagementAreaAccessAsync_Allows_Finance_Effective_Member()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCompanyWithRolesAsync(db);
        var financeUser = CreateUser("finance@test.local", "Fin", "User");
        db.Users.Add(financeUser);
        db.ManagementCompanyUsers.Add(CreateMembership(fixture.Company.Id, financeUser.Id, fixture.Roles["FINANCE"].Id, true, Today().AddDays(-3)));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AuthorizeManagementAreaAccessAsync(financeUser.Id, fixture.Company.Slug);

        Assert.True(result.IsAuthorized);
        Assert.NotNull(result.Context);
        Assert.Equal("FINANCE", result.Context!.ActorRoleCode);
    }

    [Fact]
    public async Task AuthorizeAsync_Rejects_Finance_Admin_Access()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCompanyWithRolesAsync(db);
        var financeUser = CreateUser("finance-admin@test.local", "Fin", "User");
        db.Users.Add(financeUser);
        db.ManagementCompanyUsers.Add(CreateMembership(fixture.Company.Id, financeUser.Id, fixture.Roles["FINANCE"].Id, true, Today().AddDays(-3)));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AuthorizeAsync(financeUser.Id, fixture.Company.Slug);

        Assert.False(result.IsAuthorized);
        Assert.True(result.IsForbidden);
        Assert.True(result.MembershipValidButNotAdmin);
    }

    [Fact]
    public async Task AuthorizeManagementAreaAccessAsync_Rejects_Future_Dated_Membership()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCompanyWithRolesAsync(db);
        var supportUser = CreateUser("support-future@test.local", "Sup", "Port");
        db.Users.Add(supportUser);
        db.ManagementCompanyUsers.Add(CreateMembership(fixture.Company.Id, supportUser.Id, fixture.Roles["SUPPORT"].Id, true, Today().AddDays(2)));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AuthorizeManagementAreaAccessAsync(supportUser.Id, fixture.Company.Slug);

        Assert.False(result.IsAuthorized);
        Assert.True(result.MembershipNotEffective);
    }

    [Fact]
    public async Task GetAddRoleOptionsAsync_Does_Not_Expose_Owner()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCompanyWithActorAsync(db, "OWNER");
        var service = CreateService(db);

        var options = await service.GetAddRoleOptionsAsync(fixture.Context);

        Assert.DoesNotContain(options, x => x.RoleCode == "OWNER");
        Assert.Contains(options, x => x.RoleCode == "MANAGER");
        Assert.Contains(options, x => x.RoleCode == "FINANCE");
        Assert.Contains(options, x => x.RoleCode == "SUPPORT");
    }

    [Fact]
    public async Task UpdateMembershipAsync_Blocks_Self_Role_Change()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCompanyWithActorAsync(db, "MANAGER");
        var service = CreateService(db);

        var result = await service.UpdateMembershipAsync(
            fixture.Context,
            fixture.Context.ActorMembershipId,
            new ManagementUserUpdateRequest
            {
                RoleId = fixture.Roles["FINANCE"].Id,
                JobTitle = "Changed",
                IsActive = true,
                ValidFrom = Today().AddDays(-2)
            });

        Assert.False(result.Success);
        Assert.True(result.CannotChangeOwnRole);
    }

    [Fact]
    public async Task UpdateMembershipAsync_Blocks_Self_Deactivation()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCompanyWithActorAsync(db, "MANAGER");
        var service = CreateService(db);

        var result = await service.UpdateMembershipAsync(
            fixture.Context,
            fixture.Context.ActorMembershipId,
            new ManagementUserUpdateRequest
            {
                RoleId = fixture.Roles["MANAGER"].Id,
                JobTitle = "Changed",
                IsActive = false,
                ValidFrom = Today().AddDays(-2)
            });

        Assert.False(result.Success);
        Assert.True(result.CannotDeactivateSelf);
    }

    [Fact]
    public async Task UpdateMembershipAsync_Blocks_Generic_Assignment_Of_Owner()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCompanyWithActorAndTargetAsync(db, "OWNER", "FINANCE");
        var service = CreateService(db);

        var result = await service.UpdateMembershipAsync(
            fixture.Context,
            fixture.TargetMembershipId,
            new ManagementUserUpdateRequest
            {
                RoleId = fixture.Roles["OWNER"].Id,
                JobTitle = "Promoted",
                IsActive = true,
                ValidFrom = Today().AddDays(-2)
            });

        Assert.False(result.Success);
        Assert.True(result.CannotAssignOwner);
    }

    [Fact]
    public async Task DeleteMembershipAsync_Blocks_Deleting_Owner()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCompanyWithOwnerAndManagerAsync(db);
        var service = CreateService(db);

        var managerAuthorization = await service.AuthorizeAsync(fixture.ManagerUser.Id, fixture.Company.Slug);
        var result = await service.DeleteMembershipAsync(managerAuthorization.Context!, fixture.OwnerMembershipId);

        Assert.False(result.Success);
        Assert.True(result.CannotDeleteOwner);
    }

    [Fact]
    public async Task TransferOwnershipAsync_Requires_Current_Owner()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCompanyWithActorAndTargetAsync(db, "MANAGER", "FINANCE");
        var service = CreateService(db);

        var result = await service.TransferOwnershipAsync(
            fixture.Context,
            new TransferOwnershipRequest { TargetMembershipId = fixture.TargetMembershipId });

        Assert.False(result.Success);
        Assert.True(result.Forbidden);
    }

    [Fact]
    public async Task TransferOwnershipAsync_Rejects_Inactive_Target()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCompanyWithActorAndTargetAsync(db, "OWNER", "MANAGER", targetIsActive: false);
        var service = CreateService(db);

        var result = await service.TransferOwnershipAsync(
            fixture.Context,
            new TransferOwnershipRequest { TargetMembershipId = fixture.TargetMembershipId });

        Assert.False(result.Success);
        Assert.True(result.TargetNotEligibleForOwnership);
    }

    [Fact]
    public async Task TransferOwnershipAsync_Atomically_Swaps_Owner_To_Target()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCompanyWithActorAndTargetAsync(db, "OWNER", "MANAGER");
        var service = CreateService(db);

        var result = await service.TransferOwnershipAsync(
            fixture.Context,
            new TransferOwnershipRequest { TargetMembershipId = fixture.TargetMembershipId });

        Assert.True(result.Success);

        var memberships = await db.ManagementCompanyUsers
            .AsNoTracking()
            .Where(x => x.ManagementCompanyId == fixture.Company.Id)
            .OrderBy(x => x.Id)
            .ToListAsync();

        var ownerRoleId = fixture.Roles["OWNER"].Id;
        var managerRoleId = fixture.Roles["MANAGER"].Id;

        Assert.Single(memberships.Where(x => x.ManagementCompanyRoleId == ownerRoleId && x.IsActive));
        Assert.Equal(ownerRoleId, memberships.Single(x => x.Id == fixture.TargetMembershipId).ManagementCompanyRoleId);
        Assert.Equal(managerRoleId, memberships.Single(x => x.Id == fixture.Context.ActorMembershipId).ManagementCompanyRoleId);
    }

    [Fact]
    public async Task TransferOwnershipAsync_Rejects_Cross_Company_Target()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCompanyWithActorAndTargetAsync(db, "OWNER", "MANAGER");
        var otherCompany = new ManagementCompany
        {
            Id = Guid.NewGuid(),
            Name = "Other Co",
            Slug = "other-co",
            RegistryCode = "REG-OTHER",
            VatNumber = "VAT-OTHER",
            Email = "other@test.local",
            Phone = "1234567",
            Address = "Other Street 1",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        var otherUser = CreateUser("othermember@test.local", "Other", "Member");
        db.ManagementCompanies.Add(otherCompany);
        db.Users.Add(otherUser);
        db.ManagementCompanyUsers.Add(CreateMembership(otherCompany.Id, otherUser.Id, fixture.Roles["MANAGER"].Id, true, Today().AddDays(-3)));
        await db.SaveChangesAsync();

        var crossCompanyTargetId = await db.ManagementCompanyUsers
            .Where(x => x.ManagementCompanyId == otherCompany.Id)
            .Select(x => x.Id)
            .SingleAsync();

        var service = CreateService(db);
        var result = await service.TransferOwnershipAsync(
            fixture.Context,
            new TransferOwnershipRequest { TargetMembershipId = crossCompanyTargetId });

        Assert.False(result.Success);
        Assert.True(result.NotFound);
    }

    private static ManagementUserAdminService CreateService(AppDbContext dbContext)
    {
        var joinRequestService = new Mock<ICompanyJoinRequestService>();
        joinRequestService
            .Setup(x => x.ListPendingForCompanyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CompanyJoinRequestListItem>());

        return new ManagementUserAdminService(
            dbContext,
            joinRequestService.Object,
            NullLogger<ManagementUserAdminService>.Instance);
    }

    private static async Task<(ManagementCompany Company, Dictionary<string, ManagementCompanyRole> Roles)> SeedCompanyWithRolesAsync(AppDbContext db)
    {
        var company = new ManagementCompany
        {
            Id = Guid.NewGuid(),
            Name = "Acme Management",
            Slug = "acme-management",
            RegistryCode = Guid.NewGuid().ToString("N"),
            VatNumber = Guid.NewGuid().ToString("N"),
            Email = "company@test.local",
            Phone = "5551234",
            Address = "Main Street 1",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var roles = new[]
        {
            CreateRole("OWNER"),
            CreateRole("MANAGER"),
            CreateRole("FINANCE"),
            CreateRole("SUPPORT")
        };

        db.ManagementCompanies.Add(company);
        db.ManagementCompanyRoles.AddRange(roles);
        await db.SaveChangesAsync();

        return (company, roles.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase));
    }

    private static async Task<(ManagementCompany Company, Dictionary<string, ManagementCompanyRole> Roles, ManagementUserAdminAuthorizedContext Context)> SeedCompanyWithActorAsync(
        AppDbContext db,
        string actorRoleCode)
    {
        var fixture = await SeedCompanyWithRolesAsync(db);
        var actor = CreateUser($"{actorRoleCode.ToLowerInvariant()}-actor@test.local", "Actor", actorRoleCode);
        db.Users.Add(actor);

        var actorMembership = CreateMembership(fixture.Company.Id, actor.Id, fixture.Roles[actorRoleCode].Id, true, Today().AddDays(-2));
        db.ManagementCompanyUsers.Add(actorMembership);
        await db.SaveChangesAsync();

        return (fixture.Company, fixture.Roles, new ManagementUserAdminAuthorizedContext
        {
            AppUserId = actor.Id,
            ManagementCompanyId = fixture.Company.Id,
            CompanySlug = fixture.Company.Slug,
            CompanyName = fixture.Company.Name,
            ActorMembershipId = actorMembership.Id,
            ActorRoleId = fixture.Roles[actorRoleCode].Id,
            ActorRoleCode = actorRoleCode,
            ActorRoleLabel = actorRoleCode,
            IsOwner = actorRoleCode == "OWNER",
            IsAdmin = actorRoleCode is "OWNER" or "MANAGER",
            ValidFrom = actorMembership.ValidFrom,
            ValidTo = actorMembership.ValidTo
        });
    }

    private static async Task<(ManagementCompany Company, Dictionary<string, ManagementCompanyRole> Roles, ManagementUserAdminAuthorizedContext Context, Guid TargetMembershipId)> SeedCompanyWithActorAndTargetAsync(
        AppDbContext db,
        string actorRoleCode,
        string targetRoleCode,
        bool targetIsActive = true)
    {
        var fixture = await SeedCompanyWithActorAsync(db, actorRoleCode);
        var targetUser = CreateUser($"{targetRoleCode.ToLowerInvariant()}-target@test.local", "Target", targetRoleCode);
        db.Users.Add(targetUser);
        var targetMembership = CreateMembership(fixture.Company.Id, targetUser.Id, fixture.Roles[targetRoleCode].Id, targetIsActive, Today().AddDays(-2));
        db.ManagementCompanyUsers.Add(targetMembership);
        await db.SaveChangesAsync();

        return (fixture.Company, fixture.Roles, fixture.Context, targetMembership.Id);
    }

    private static async Task<(ManagementCompany Company, AppUser OwnerUser, Guid OwnerMembershipId, AppUser ManagerUser)> SeedCompanyWithOwnerAndManagerAsync(AppDbContext db)
    {
        var fixture = await SeedCompanyWithRolesAsync(db);
        var ownerUser = CreateUser("owner@test.local", "Own", "Er");
        var managerUser = CreateUser("manager@test.local", "Man", "Ager");
        db.Users.AddRange(ownerUser, managerUser);

        var ownerMembership = CreateMembership(fixture.Company.Id, ownerUser.Id, fixture.Roles["OWNER"].Id, true, Today().AddDays(-2));
        var managerMembership = CreateMembership(fixture.Company.Id, managerUser.Id, fixture.Roles["MANAGER"].Id, true, Today().AddDays(-2));
        db.ManagementCompanyUsers.AddRange(ownerMembership, managerMembership);
        await db.SaveChangesAsync();

        return (fixture.Company, ownerUser, ownerMembership.Id, managerUser);
    }

    private static ManagementCompanyRole CreateRole(string code)
    {
        return new ManagementCompanyRole
        {
            Id = Guid.NewGuid(),
            Code = code,
            Label = new LangStr(code)
        };
    }

    private static AppUser CreateUser(string email, string firstName, string lastName)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
    }

    private static ManagementCompanyUser CreateMembership(Guid companyId, Guid userId, Guid roleId, bool isActive, DateOnly validFrom)
    {
        return new ManagementCompanyUser
        {
            Id = Guid.NewGuid(),
            ManagementCompanyId = companyId,
            AppUserId = userId,
            ManagementCompanyRoleId = roleId,
            JobTitle = new LangStr("Member"),
            IsActive = isActive,
            ValidFrom = validFrom,
            ValidTo = null,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);
}

