 using App.BLL.ManagementUsers;
using App.BLL.Onboarding;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Onboarding.Tests;

public class ManagementUserAdminServiceTests
{
    [Fact]
    public async Task AuthorizeAsync_ReturnsAuthorized_ForOwnerInSameCompany()
    {
        await using var dbContext = CreateDbContext();
        var appUser = CreateUser("owner@test.com", "Owner", "User");
        var ownerRole = CreateRole("OWNER", "Owner");
        var company = CreateCompany("north-estate", "North Estate");

        dbContext.Users.Add(appUser);
        dbContext.ManagementCompanyRoles.Add(ownerRole);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(company.Id, appUser.Id, ownerRole.Id, true, "Owner"));
        await dbContext.SaveChangesAsync();

        var sut = CreateSut(dbContext);

        var result = await sut.AuthorizeAsync(appUser.Id, company.Slug);

        Assert.True(result.IsAuthorized);
        Assert.NotNull(result.Context);
        Assert.Equal(company.Id, result.Context!.ManagementCompanyId);
    }

    [Fact]
    public async Task AuthorizeAsync_ReturnsForbidden_ForNonManagerRole()
    {
        await using var dbContext = CreateDbContext();
        var appUser = CreateUser("staff@test.com", "Staff", "User");
        var supportRole = CreateRole("SUPPORT", "Support specialist");
        var company = CreateCompany("north-estate", "North Estate");

        dbContext.Users.Add(appUser);
        dbContext.ManagementCompanyRoles.Add(supportRole);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(company.Id, appUser.Id, supportRole.Id, true, "Support"));
        await dbContext.SaveChangesAsync();

        var sut = CreateSut(dbContext);

        var result = await sut.AuthorizeAsync(appUser.Id, company.Slug);

        Assert.True(result.IsForbidden);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public async Task ListCompanyMembersAsync_ReturnsOnlyUsersFromCurrentCompany()
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateUser("owner@test.com", "Owner", "User");
        var targetUser = CreateUser("member@test.com", "Member", "User");
        var otherCompanyUser = CreateUser("other@test.com", "Other", "User");
        var ownerRole = CreateRole("OWNER", "Owner");
        var managerRole = CreateRole("MANAGER", "Manager");
        var company = CreateCompany("north-estate", "North Estate");
        var otherCompany = CreateCompany("south-estate", "South Estate");

        dbContext.Users.AddRange(actor, targetUser, otherCompanyUser);
        dbContext.ManagementCompanyRoles.AddRange(ownerRole, managerRole);
        dbContext.ManagementCompanies.AddRange(company, otherCompany);
        dbContext.ManagementCompanyUsers.AddRange(
            CreateMembership(company.Id, actor.Id, ownerRole.Id, true, "Owner"),
            CreateMembership(company.Id, targetUser.Id, managerRole.Id, true, "Manager"),
            CreateMembership(otherCompany.Id, otherCompanyUser.Id, managerRole.Id, true, "Manager"));
        await dbContext.SaveChangesAsync();

        var sut = CreateSut(dbContext);
        var auth = await sut.AuthorizeAsync(actor.Id, company.Slug);

        var result = await sut.ListCompanyMembersAsync(auth.Context!);

        Assert.Equal(2, result.Members.Count);
        Assert.DoesNotContain(result.Members, x => x.Email == otherCompanyUser.Email);
    }

    [Fact]
    public async Task AddUserByEmailAsync_CreatesMembership_ForExistingAppUserOnly()
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateUser("owner@test.com", "Owner", "User");
        var targetUser = CreateUser("member@test.com", "Member", "User");
        var ownerRole = CreateRole("OWNER", "Owner");
        var managerRole = CreateRole("MANAGER", "Manager");
        var company = CreateCompany("north-estate", "North Estate");

        dbContext.Users.AddRange(actor, targetUser);
        dbContext.ManagementCompanyRoles.AddRange(ownerRole, managerRole);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(company.Id, actor.Id, ownerRole.Id, true, "Owner"));
        await dbContext.SaveChangesAsync();

        var sut = CreateSut(dbContext);
        var auth = await sut.AuthorizeAsync(actor.Id, company.Slug);

        var result = await sut.AddUserByEmailAsync(auth.Context!, new ManagementUserAddRequest
        {
            Email = targetUser.Email!,
            RoleId = managerRole.Id,
            JobTitle = "Property manager",
            IsActive = true,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        Assert.True(result.Success);
        Assert.Equal(2, await dbContext.ManagementCompanyUsers.CountAsync());
        Assert.Equal(1, await dbContext.Users.CountAsync(x => x.Id == targetUser.Id));
    }

    [Fact]
    public async Task AddUserByEmailAsync_RejectsDuplicateMembership()
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateUser("owner@test.com", "Owner", "User");
        var targetUser = CreateUser("member@test.com", "Member", "User");
        var ownerRole = CreateRole("OWNER", "Owner");
        var managerRole = CreateRole("MANAGER", "Manager");
        var company = CreateCompany("north-estate", "North Estate");

        dbContext.Users.AddRange(actor, targetUser);
        dbContext.ManagementCompanyRoles.AddRange(ownerRole, managerRole);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyUsers.AddRange(
            CreateMembership(company.Id, actor.Id, ownerRole.Id, true, "Owner"),
            CreateMembership(company.Id, targetUser.Id, managerRole.Id, true, "Manager"));
        await dbContext.SaveChangesAsync();

        var sut = CreateSut(dbContext);
        var auth = await sut.AuthorizeAsync(actor.Id, company.Slug);

        var result = await sut.AddUserByEmailAsync(auth.Context!, new ManagementUserAddRequest
        {
            Email = targetUser.Email!,
            RoleId = managerRole.Id,
            JobTitle = "Property manager",
            IsActive = true,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        Assert.True(result.DuplicateMembership);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateMembershipAsync_UpdatesOnlyMembershipInsideCurrentCompany()
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateUser("owner@test.com", "Owner", "User");
        var targetUser = CreateUser("member@test.com", "Member", "User");
        var ownerRole = CreateRole("OWNER", "Owner");
        var managerRole = CreateRole("MANAGER", "Manager");
        var financeRole = CreateRole("FINANCE", "Finance");
        var company = CreateCompany("north-estate", "North Estate");
        var membership = CreateMembership(company.Id, targetUser.Id, managerRole.Id, true, "Manager");

        dbContext.Users.AddRange(actor, targetUser);
        dbContext.ManagementCompanyRoles.AddRange(ownerRole, managerRole, financeRole);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyUsers.AddRange(
            CreateMembership(company.Id, actor.Id, ownerRole.Id, true, "Owner"),
            membership);
        await dbContext.SaveChangesAsync();

        var sut = CreateSut(dbContext);
        var auth = await sut.AuthorizeAsync(actor.Id, company.Slug);

        var result = await sut.UpdateMembershipAsync(auth.Context!, membership.Id, new ManagementUserUpdateRequest
        {
            RoleId = financeRole.Id,
            JobTitle = "Finance lead",
            IsActive = false,
            ValidFrom = membership.ValidFrom,
            ValidTo = membership.ValidFrom.AddDays(30)
        });

        var updated = await dbContext.ManagementCompanyUsers.SingleAsync(x => x.Id == membership.Id);
        Assert.True(result.Success);
        Assert.Equal(financeRole.Id, updated.ManagementCompanyRoleId);
        Assert.Equal("Finance lead", updated.JobTitle);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task DeleteMembershipAsync_RemovesOnlyCurrentCompanyMembership_AndNotAppUser()
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateUser("owner@test.com", "Owner", "User");
        var targetUser = CreateUser("member@test.com", "Member", "User");
        var ownerRole = CreateRole("OWNER", "Owner");
        var managerRole = CreateRole("MANAGER", "Manager");
        var company = CreateCompany("north-estate", "North Estate");
        var membership = CreateMembership(company.Id, targetUser.Id, managerRole.Id, true, "Manager");

        dbContext.Users.AddRange(actor, targetUser);
        dbContext.ManagementCompanyRoles.AddRange(ownerRole, managerRole);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyUsers.AddRange(
            CreateMembership(company.Id, actor.Id, ownerRole.Id, true, "Owner"),
            membership);
        await dbContext.SaveChangesAsync();

        var sut = CreateSut(dbContext);
        var auth = await sut.AuthorizeAsync(actor.Id, company.Slug);

        var result = await sut.DeleteMembershipAsync(auth.Context!, membership.Id);

        Assert.True(result.Success);
        Assert.Single(await dbContext.ManagementCompanyUsers.ToListAsync());
        Assert.True(await dbContext.Users.AnyAsync(x => x.Id == targetUser.Id));
    }

    [Fact]
    public async Task GetPendingAccessRequestsAsync_ReturnsCompanyPendingRequestsOnly()
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateUser("owner@test.com", "Owner", "User");
        var requester = CreateUser("requester@test.com", "Requester", "User");
        var otherRequester = CreateUser("other-requester@test.com", "Other", "Requester");
        var ownerRole = CreateRole("OWNER", "Owner");
        var managerRole = CreateRole("MANAGER", "Manager");
        var company = CreateCompany("north-estate", "North Estate");
        var otherCompany = CreateCompany("south-estate", "South Estate");

        dbContext.Users.AddRange(actor, requester, otherRequester);
        dbContext.ManagementCompanyRoles.AddRange(ownerRole, managerRole);
        dbContext.ManagementCompanies.AddRange(company, otherCompany);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(company.Id, actor.Id, ownerRole.Id, true, "Owner"));
        dbContext.ManagementCompanyJoinRequests.AddRange(
            new ManagementCompanyJoinRequest
            {
                Id = Guid.NewGuid(),
                AppUserId = requester.Id,
                ManagementCompanyId = company.Id,
                RequestedManagementCompanyRoleId = managerRole.Id,
                Status = ManagementCompanyJoinRequestStatus.Pending,
                Message = "Please approve",
                CreatedAt = DateTime.UtcNow
            },
            new ManagementCompanyJoinRequest
            {
                Id = Guid.NewGuid(),
                AppUserId = otherRequester.Id,
                ManagementCompanyId = otherCompany.Id,
                RequestedManagementCompanyRoleId = managerRole.Id,
                Status = ManagementCompanyJoinRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
        await dbContext.SaveChangesAsync();

        var sut = CreateSut(dbContext);
        var auth = await sut.AuthorizeAsync(actor.Id, company.Slug);

        var result = await sut.GetPendingAccessRequestsAsync(auth.Context!);

        Assert.Single(result.Requests);
        Assert.Equal(requester.Email, result.Requests[0].RequesterEmail);
    }

    [Fact]
    public async Task ApprovePendingAccessRequestAsync_CreatesMembership_AndResolvesRequest()
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateUser("owner@test.com", "Owner", "User");
        var requester = CreateUser("requester@test.com", "Requester", "User");
        var ownerRole = CreateRole("OWNER", "Owner");
        var managerRole = CreateRole("MANAGER", "Manager");
        var company = CreateCompany("north-estate", "North Estate");

        dbContext.Users.AddRange(actor, requester);
        dbContext.ManagementCompanyRoles.AddRange(ownerRole, managerRole);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(company.Id, actor.Id, ownerRole.Id, true, "Owner"));
        var request = new ManagementCompanyJoinRequest
        {
            Id = Guid.NewGuid(),
            AppUserId = requester.Id,
            ManagementCompanyId = company.Id,
            RequestedManagementCompanyRoleId = managerRole.Id,
            Status = ManagementCompanyJoinRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.ManagementCompanyJoinRequests.Add(request);
        await dbContext.SaveChangesAsync();

        var sut = CreateSut(dbContext);
        var auth = await sut.AuthorizeAsync(actor.Id, company.Slug);

        var result = await sut.ApprovePendingAccessRequestAsync(auth.Context!, request.Id);

        Assert.True(result.Success);
        Assert.True(await dbContext.ManagementCompanyUsers.AnyAsync(x => x.AppUserId == requester.Id && x.ManagementCompanyId == company.Id));
        var storedRequest = await dbContext.ManagementCompanyJoinRequests.SingleAsync(x => x.Id == request.Id);
        Assert.Equal(ManagementCompanyJoinRequestStatus.Approved, storedRequest.Status);
    }

    [Fact]
    public async Task RejectPendingAccessRequestAsync_ResolvesRequestWithoutMembershipCreation()
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateUser("owner@test.com", "Owner", "User");
        var requester = CreateUser("requester@test.com", "Requester", "User");
        var ownerRole = CreateRole("OWNER", "Owner");
        var managerRole = CreateRole("MANAGER", "Manager");
        var company = CreateCompany("north-estate", "North Estate");

        dbContext.Users.AddRange(actor, requester);
        dbContext.ManagementCompanyRoles.AddRange(ownerRole, managerRole);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(company.Id, actor.Id, ownerRole.Id, true, "Owner"));
        var request = new ManagementCompanyJoinRequest
        {
            Id = Guid.NewGuid(),
            AppUserId = requester.Id,
            ManagementCompanyId = company.Id,
            RequestedManagementCompanyRoleId = managerRole.Id,
            Status = ManagementCompanyJoinRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.ManagementCompanyJoinRequests.Add(request);
        await dbContext.SaveChangesAsync();

        var sut = CreateSut(dbContext);
        var auth = await sut.AuthorizeAsync(actor.Id, company.Slug);

        var result = await sut.RejectPendingAccessRequestAsync(auth.Context!, request.Id);

        Assert.True(result.Success);
        Assert.False(await dbContext.ManagementCompanyUsers.AnyAsync(x => x.AppUserId == requester.Id && x.ManagementCompanyId == company.Id));
        var storedRequest = await dbContext.ManagementCompanyJoinRequests.SingleAsync(x => x.Id == request.Id);
        Assert.Equal(ManagementCompanyJoinRequestStatus.Rejected, storedRequest.Status);
    }

    private static ManagementUserAdminService CreateSut(AppDbContext dbContext)
    {
        var joinRequestService = new ManagementCompanyJoinRequestService(dbContext, NullLogger<ManagementCompanyJoinRequestService>.Instance);
        return new ManagementUserAdminService(dbContext, joinRequestService, NullLogger<ManagementUserAdminService>.Instance);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AppUser CreateUser(string email, string firstName, string lastName)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };
    }

    private static ManagementCompanyRole CreateRole(string code, string label)
    {
        return new ManagementCompanyRole
        {
            Id = Guid.NewGuid(),
            Code = code,
            Label = label
        };
    }

    private static ManagementCompany CreateCompany(string slug, string name)
    {
        return new ManagementCompany
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = name,
            RegistryCode = $"REG-{Guid.NewGuid():N}"[..16],
            VatNumber = $"VAT-{Guid.NewGuid():N}"[..16],
            Email = $"{slug}@test.com",
            Phone = "+3720000000",
            Address = "Test address 1",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static ManagementCompanyUser CreateMembership(Guid companyId, Guid appUserId, Guid roleId, bool isActive, string jobTitle)
    {
        return new ManagementCompanyUser
        {
            Id = Guid.NewGuid(),
            ManagementCompanyId = companyId,
            AppUserId = appUserId,
            ManagementCompanyRoleId = roleId,
            JobTitle = jobTitle,
            IsActive = isActive,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidTo = null,
            CreatedAt = DateTime.UtcNow
        };
    }
}
