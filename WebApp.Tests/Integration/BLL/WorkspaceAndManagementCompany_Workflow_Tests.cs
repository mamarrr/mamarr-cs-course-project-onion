using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.ManagementCompanies;
using App.BLL.DTO.Workspace.Models;
using App.BLL.DTO.Workspace.Queries;
using App.DAL.EF;
using App.Domain.Identity;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.BLL;

public class WorkspaceAndManagementCompany_Workflow_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WorkspaceAndManagementCompany_Workflow_Tests(CustomWebApplicationFactory factory)
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

    [Fact]
    public async Task CreateCompany_CreatesOwnerMembershipAndUniqueSlug()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var user = await CreateUserAsync(db, "create-company");
        var registryCode = UniqueCode("REG");

        var created = await bll.ManagementCompanies.CreateAsync(user.Id, new ManagementCompanyBllDto
        {
            Name = "Company A Maintenance",
            RegistryCode = registryCode,
            VatNumber = "EE123456789",
            Email = "created-company@test.ee",
            Phone = "+372 5555 9001",
            Address = "Created Street 1"
        });

        created.IsSuccess.Should().BeTrue();
        created.Value.Id.Should().NotBeEmpty();
        created.Value.Slug.Should().StartWith("company-a-maintenance");
        created.Value.Slug.Should().NotBe(TestTenants.CompanyASlug);

        var roleCode = await db.ManagementCompanyUsers
            .AsNoTracking()
            .Where(membership => membership.ManagementCompanyId == created.Value.Id && membership.AppUserId == user.Id)
            .Select(membership => membership.ManagementCompanyRole!.Code)
            .SingleAsync();
        roleCode.Should().Be("OWNER");

        var workspace = await bll.Workspaces.GetDefaultManagementCompanySlugAsync(user.Id);
        workspace.Value.Should().Be(created.Value.Slug);
    }

    [Fact]
    public async Task CreateCompany_RejectsDuplicateRegistryCodeAndEmptyUser()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var user = await CreateUserAsync(db, "duplicate-company");

        var duplicateRegistry = await bll.ManagementCompanies.CreateAsync(user.Id, new ManagementCompanyBllDto
        {
            Name = "Duplicate Company",
            RegistryCode = "TEST-COMPANY-A",
            VatNumber = "EE123456789",
            Email = "duplicate-company@test.ee",
            Phone = "+372 5555 9002",
            Address = "Duplicate Street 1"
        });
        var unauthenticated = await bll.ManagementCompanies.CreateAsync(Guid.Empty, new ManagementCompanyBllDto
        {
            Name = "No User Company",
            RegistryCode = UniqueCode("REG"),
            VatNumber = "EE123456780",
            Email = "no-user-company@test.ee",
            Phone = "+372 5555 9003",
            Address = "No User Street 1"
        });

        duplicateRegistry.IsFailed.Should().BeTrue();
        duplicateRegistry.Errors.Should().Contain(error => error is ConflictError);
        unauthenticated.IsFailed.Should().BeTrue();
        unauthenticated.Errors.Should().Contain(error => error is UnauthorizedError);
    }

    [Fact]
    public async Task UpdateCompany_ChangesProfileAndRejectsUnauthorizedUser()
    {
        Guid userId;
        string companySlug;
        string registryCode;

        using (var createScope = _factory.Services.CreateScope())
        {
            var db = createScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bll = createScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var user = await CreateUserAsync(db, "update-company");
            var created = await bll.ManagementCompanies.CreateAsync(user.Id, new ManagementCompanyBllDto
            {
                Name = "Update Company",
                RegistryCode = UniqueCode("REG"),
                VatNumber = "EE123456781",
                Email = "update-company@test.ee",
                Phone = "+372 5555 9004",
                Address = "Update Street 1"
            });
            created.IsSuccess.Should().BeTrue();

            userId = user.Id;
            companySlug = created.Value.Slug;
            registryCode = created.Value.RegistryCode;
        }

        using var scope = _factory.Services.CreateScope();
        var _bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var updated = await _bll.ManagementCompanies.UpdateAndGetProfileAsync(
            new ManagementCompanyRoute { AppUserId = userId, CompanySlug = companySlug },
            new ManagementCompanyBllDto
            {
                Name = "Updated Company",
                RegistryCode = registryCode,
                VatNumber = "EE123456782",
                Email = "updated-company@test.ee",
                Phone = "+372 5555 9005",
                Address = "Updated Street 1"
            });
        var unauthorized = await _bll.ManagementCompanies.UpdateAsync(
            new ManagementCompanyRoute { AppUserId = TestUsers.SystemAdminId, CompanySlug = companySlug },
            new ManagementCompanyBllDto
            {
                Name = "Blocked Update",
                RegistryCode = registryCode,
                VatNumber = "EE123456783",
                Email = "blocked-company@test.ee",
                Phone = "+372 5555 9006",
                Address = "Blocked Street 1"
            });

        updated.IsSuccess.Should().BeTrue();
        updated.Value.Name.Should().Be("Updated Company");
        updated.Value.Email.Should().Be("updated-company@test.ee");
        unauthorized.IsFailed.Should().BeTrue();
        unauthorized.Errors.Should().Contain(error => error is ForbiddenError);
    }

    [Fact]
    public async Task CreateCompany_RejectsValidationErrors()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var user = await CreateUserAsync(db, "invalid-company");

        var result = await bll.ManagementCompanies.CreateAsync(user.Id, new ManagementCompanyBllDto
        {
            Name = " ",
            RegistryCode = UniqueCode("REG"),
            VatNumber = "EE123456784",
            Email = "invalid-company@test.ee",
            Phone = "+372 5555 9007",
            Address = "Invalid Street 1"
        });

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(error => error is ValidationAppError);
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

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }
}
