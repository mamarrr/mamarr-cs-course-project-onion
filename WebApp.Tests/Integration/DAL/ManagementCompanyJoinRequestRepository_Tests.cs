using App.DAL.Contracts;
using App.DAL.DTO.ManagementCompanies;
using App.DAL.EF;
using App.Domain.Identity;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.DAL;

public class ManagementCompanyJoinRequestRepository_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ManagementCompanyJoinRequestRepository_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PendingByCompanyAsync_ReturnsPendingRequestsWithRequesterAndLookupData()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var ids = await LookupIdsAsync(db);
        var user = await CreateUserAsync(db, "pending");
        var requestId = await CreateJoinRequestAsync(uow, user.Id, ids.OwnerRoleId, ids.PendingStatusId, "Please add me");

        var pending = await uow.ManagementCompanyJoinRequests.PendingByCompanyAsync(
            TestTenants.CompanyAId,
            ids.PendingStatusId);

        pending.Should().ContainSingle(request => request.Id == requestId);
        var request = pending.Single(request => request.Id == requestId);
        request.RequesterEmail.Should().Be(user.Email);
        request.RequestedRoleCode.Should().Be("OWNER");
        request.StatusCode.Should().Be("PENDING");
        request.Message.Should().Be("Please add me");
    }

    [Fact]
    public async Task FindAndHasPendingRequest_AreCompanyAndStatusScoped()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var ids = await LookupIdsAsync(db);
        var user = await CreateUserAsync(db, "find");
        var requestId = await CreateJoinRequestAsync(uow, user.Id, ids.OwnerRoleId, ids.PendingStatusId, "Pending");

        var found = await uow.ManagementCompanyJoinRequests.FindByIdAndCompanyAsync(requestId, TestTenants.CompanyAId);
        var wrongCompany = await uow.ManagementCompanyJoinRequests.FindByIdAndCompanyAsync(requestId, Guid.NewGuid());
        var hasPending = await uow.ManagementCompanyJoinRequests.HasPendingRequestAsync(
            user.Id,
            TestTenants.CompanyAId,
            ids.PendingStatusId);
        var wrongStatus = await uow.ManagementCompanyJoinRequests.HasPendingRequestAsync(
            user.Id,
            TestTenants.CompanyAId,
            ids.ApprovedStatusId);

        found.Should().NotBeNull();
        found!.Id.Should().Be(requestId);
        wrongCompany.Should().BeNull();
        hasPending.Should().BeTrue();
        wrongStatus.Should().BeFalse();
    }

    [Fact]
    public async Task SetStatusAsync_UpdatesStatusAndResolutionFields()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var ids = await LookupIdsAsync(db);
        var resolvedAt = new DateTime(2026, 5, 12, 12, 0, 0, DateTimeKind.Utc);
        var user = await CreateUserAsync(db, "resolve");
        var requestId = await CreateJoinRequestAsync(uow, user.Id, ids.OwnerRoleId, ids.PendingStatusId, "Resolve");

        var updated = await uow.ManagementCompanyJoinRequests.SetStatusAsync(
            requestId,
            TestTenants.CompanyAId,
            ids.ApprovedStatusId,
            TestUsers.CompanyAOwnerId,
            resolvedAt);
        var wrongCompany = await uow.ManagementCompanyJoinRequests.SetStatusAsync(
            requestId,
            Guid.NewGuid(),
            ids.ApprovedStatusId,
            TestUsers.CompanyAOwnerId,
            resolvedAt);
        await uow.SaveChangesAsync(CancellationToken.None);

        updated.Should().BeTrue();
        wrongCompany.Should().BeFalse();
        var persisted = await uow.ManagementCompanyJoinRequests.FindByIdAndCompanyAsync(requestId, TestTenants.CompanyAId);
        persisted.Should().NotBeNull();
        persisted!.StatusCode.Should().Be("APPROVED");
        persisted.ResolvedAt.Should().Be(resolvedAt);
        persisted.ResolvedByAppUserId.Should().Be(TestUsers.CompanyAOwnerId);
    }

    private static async Task<Guid> CreateJoinRequestAsync(
        IAppUOW uow,
        Guid appUserId,
        Guid roleId,
        Guid statusId,
        string message)
    {
        var id = Guid.NewGuid();
        uow.ManagementCompanyJoinRequests.Add(new ManagementCompanyJoinRequestDalDto
        {
            Id = id,
            AppUserId = appUserId,
            ManagementCompanyId = TestTenants.CompanyAId,
            RequestedRoleId = roleId,
            StatusId = statusId,
            Message = message
        });
        await uow.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<JoinRequestLookupIds> LookupIdsAsync(AppDbContext db)
    {
        var ownerRoleId = await db.ManagementCompanyRoles
            .AsNoTracking()
            .Where(role => role.Code == "OWNER")
            .Select(role => role.Id)
            .SingleAsync();
        var pendingStatusId = await db.ManagementCompanyJoinRequestStatuses
            .AsNoTracking()
            .Where(status => status.Code == "PENDING")
            .Select(status => status.Id)
            .SingleAsync();
        var approvedStatusId = await db.ManagementCompanyJoinRequestStatuses
            .AsNoTracking()
            .Where(status => status.Code == "APPROVED")
            .Select(status => status.Id)
            .SingleAsync();

        return new JoinRequestLookupIds(ownerRoleId, pendingStatusId, approvedStatusId);
    }

    private static async Task<AppUser> CreateUserAsync(AppDbContext db, string suffix)
    {
        var unique = Guid.NewGuid().ToString("N");
        var email = $"join-{suffix}-{unique}@test.ee";
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = "Join",
            LastName = suffix,
            CreatedAt = DateTime.UtcNow
        };

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
        return user;
    }

    private sealed record JoinRequestLookupIds(Guid OwnerRoleId, Guid PendingStatusId, Guid ApprovedStatusId);
}
