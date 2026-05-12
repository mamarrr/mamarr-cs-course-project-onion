using App.BLL.Contracts;
using App.BLL.DTO.Workspace.Models;
using App.BLL.DTO.Workspace.Queries;
using App.DAL.EF;
using App.Domain.Identity;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.BLL;

public class Workspace_Workflow_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public Workspace_Workflow_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OwnerHasManagementWorkspaceAndDefaultEntryPoint()
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var hasContext = await bll.Workspaces.HasAnyContextAsync(TestUsers.CompanyAOwnerId);
        var defaultSlug = await bll.Workspaces.GetDefaultManagementCompanySlugAsync(TestUsers.CompanyAOwnerId);
        var catalog = await bll.Workspaces.GetUserCatalogAsync(TestUsers.CompanyAOwnerId);
        var entryPoint = await bll.Workspaces.ResolveWorkspaceEntryPointAsync(new ResolveWorkspaceEntryPointQuery
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            RememberedContext = new RememberedWorkspaceContext
            {
                ContextType = "management",
                ManagementCompanySlug = TestTenants.CompanyASlug
            }
        });

        hasContext.IsSuccess.Should().BeTrue();
        hasContext.Value.Should().BeTrue();
        defaultSlug.Value.Should().Be(TestTenants.CompanyASlug);
        catalog.Value.ManagementCompanies.Should().ContainSingle(option => option.Id == TestTenants.CompanyAId);
        catalog.Value.DefaultContext!.ContextType.Should().Be("management");
        entryPoint.Value.Should().NotBeNull();
        entryPoint.Value!.Kind.Should().Be(WorkspaceEntryPointKind.ManagementDashboard);
        entryPoint.Value.CompanySlug.Should().Be(TestTenants.CompanyASlug);
    }

    [Fact]
    public async Task UserWithoutContextHasNoWorkspaceEntryPoint()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var user = await CreateUserAsync(db, "workspace-empty");

        var hasContext = await bll.Workspaces.HasAnyContextAsync(user.Id);
        var catalog = await bll.Workspaces.GetUserCatalogAsync(user.Id);
        var entryPoint = await bll.Workspaces.ResolveWorkspaceEntryPointAsync(new ResolveWorkspaceEntryPointQuery
        {
            AppUserId = user.Id,
            RememberedContext = new RememberedWorkspaceContext
            {
                ContextType = "management",
                ManagementCompanySlug = TestTenants.CompanyASlug
            }
        });

        hasContext.Value.Should().BeFalse();
        catalog.Value.ManagementCompanies.Should().BeEmpty();
        catalog.Value.Customers.Should().BeEmpty();
        catalog.Value.Residents.Should().BeEmpty();
        catalog.Value.DefaultContext.Should().BeNull();
        entryPoint.Value.Should().BeNull();
    }

    [Fact]
    public async Task AuthorizeContextSelection_AllowsOwnedManagementCompanyAndRejectsForeignContext()
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var allowed = await bll.Workspaces.AuthorizeContextSelectionAsync(new AuthorizeContextSelectionQuery
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            ContextType = "management",
            ContextId = TestTenants.CompanyAId
        });
        var denied = await bll.Workspaces.AuthorizeContextSelectionAsync(new AuthorizeContextSelectionQuery
        {
            AppUserId = TestUsers.SystemAdminId,
            ContextType = "management",
            ContextId = TestTenants.CompanyAId
        });

        allowed.Value.Authorized.Should().BeTrue();
        allowed.Value.ManagementCompanySlug.Should().Be(TestTenants.CompanyASlug);
        denied.Value.Authorized.Should().BeFalse();
    }

    private static async Task<AppUser> CreateUserAsync(AppDbContext db, string suffix)
    {
        var unique = Guid.NewGuid().ToString("N");
        var email = $"bll-{suffix}-{unique}@test.ee";
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = "Bll",
            LastName = suffix,
            CreatedAt = DateTime.UtcNow
        };

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
        return user;
    }
}
