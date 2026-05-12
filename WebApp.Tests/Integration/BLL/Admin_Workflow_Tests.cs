using App.BLL.Contracts;
using App.BLL.DTO.Admin.Companies;
using App.BLL.DTO.Admin.Lookups;
using App.BLL.DTO.Admin.Tickets;
using App.BLL.DTO.Admin.Users;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.ManagementCompanies;
using App.DAL.EF;
using App.Domain.Identity;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.BLL;

public class Admin_Workflow_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public Admin_Workflow_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DashboardCompaniesUsersAndTickets_LoadSeededAdminData()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var dashboard = await bll.AdminDashboard.GetDashboardAsync();
        var companies = await bll.AdminCompanies.SearchCompaniesAsync(new AdminCompanySearchDto
        {
            SearchText = "Company A"
        });
        var companyDetails = await bll.AdminCompanies.GetCompanyDetailsAsync(TestTenants.CompanyAId);
        var users = await bll.AdminUsers.SearchUsersAsync(new AdminUserSearchDto
        {
            Email = TestUsers.SystemAdminEmail
        });
        var userDetails = await bll.AdminUsers.GetUserDetailsAsync(TestUsers.SystemAdminId);
        var tickets = await bll.AdminTicketMonitor.SearchTicketsAsync(new AdminTicketSearchDto
        {
            TicketNumber = "T-A-0001"
        });
        var ticketDetails = await bll.AdminTicketMonitor.GetTicketDetailsAsync(TestTenants.TicketAId);

        dashboard.Stats.TotalUsers.Should().BeGreaterThanOrEqualTo(3);
        dashboard.Stats.TotalManagementCompanies.Should().BeGreaterThanOrEqualTo(1);
        dashboard.Stats.OpenTickets.Should().BeGreaterThanOrEqualTo(1);
        companies.Companies.Should().ContainSingle(company => company.Id == TestTenants.CompanyAId);
        companyDetails.Should().NotBeNull();
        companyDetails!.Name.Should().NotBeNullOrWhiteSpace();
        companyDetails.UsersCount.Should().BeGreaterThanOrEqualTo(1);
        users.Users.Should().ContainSingle(user => user.Id == TestUsers.SystemAdminId);
        userDetails.Should().NotBeNull();
        userDetails!.HasSystemAdminRole.Should().BeTrue();
        tickets.Tickets.Should().ContainSingle(ticket => ticket.Id == TestTenants.TicketAId);
        ticketDetails.Should().NotBeNull();
        ticketDetails!.TicketNumber.Should().Be("T-A-0001");
        ticketDetails.CompanyName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UserLockWorkflow_LocksUnlocksAndRejectsProtectedAdminCases()
    {
        Guid userId;
        using (var userScope = _factory.Services.CreateScope())
        {
            var db = userScope.ServiceProvider.GetRequiredService<AppDbContext>();
            userId = (await CreateUserAsync(db, "admin-lock")).Id;
        }

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var locked = await bll.AdminUsers.LockUserAsync(userId, TestUsers.SystemAdminId);
        var lockedOnly = await bll.AdminUsers.SearchUsersAsync(new AdminUserSearchDto { LockedOnly = true });
        var unlocked = await bll.AdminUsers.UnlockUserAsync(userId, TestUsers.SystemAdminId);
        var selfLock = await bll.AdminUsers.LockUserAsync(TestUsers.SystemAdminId, TestUsers.SystemAdminId);
        var lastAdminLock = await bll.AdminUsers.LockUserAsync(TestUsers.SystemAdminId, userId);

        locked.IsSuccess.Should().BeTrue();
        locked.Value.IsLocked.Should().BeTrue();
        lockedOnly.Users.Should().Contain(user => user.Id == userId);
        unlocked.IsSuccess.Should().BeTrue();
        unlocked.Value.IsLocked.Should().BeFalse();
        selfLock.ShouldFailWith<BusinessRuleError>();
        lastAdminLock.ShouldFailWith<BusinessRuleError>();
    }

    [Fact]
    public async Task CompanyUpdateWorkflow_UpdatesCreatedCompanyAndRejectsInvalidOrDuplicateData()
    {
        var company = await CreateCompanyAsync("admin-company");

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var updated = await bll.AdminCompanies.UpdateCompanyAsync(
            company.CompanyId,
            new AdminCompanyUpdateDto
            {
                Name = "Admin Updated Company",
                RegistryCode = company.RegistryCode,
                VatNumber = "EE998877660",
                Email = "admin-updated-company@test.ee",
                Phone = "+372 5555 8101",
                Address = "Admin Updated Street 1",
                Slug = company.Slug
            });
        var invalid = await bll.AdminCompanies.UpdateCompanyAsync(
            company.CompanyId,
            new AdminCompanyUpdateDto
            {
                Name = " ",
                RegistryCode = company.RegistryCode,
                VatNumber = "EE998877660",
                Email = "admin-invalid-company@test.ee",
                Phone = "+372 5555 8102",
                Address = "Admin Invalid Street 1",
                Slug = company.Slug
            });
        var duplicateRegistry = await bll.AdminCompanies.UpdateCompanyAsync(
            company.CompanyId,
            new AdminCompanyUpdateDto
            {
                Name = "Duplicate Registry Company",
                RegistryCode = "TEST-COMPANY-A",
                VatNumber = "EE998877660",
                Email = "admin-duplicate-company@test.ee",
                Phone = "+372 5555 8103",
                Address = "Admin Duplicate Street 1",
                Slug = company.Slug
            });

        updated.IsSuccess.Should().BeTrue();
        updated.Value.Name.Should().Be("Admin Updated Company");
        updated.Value.Email.Should().Be("admin-updated-company@test.ee");
        invalid.ShouldFailWith<ValidationAppError>();
        duplicateRegistry.ShouldFailWith<ConflictError>();
    }

    [Fact]
    public async Task LookupWorkflow_CreatesUpdatesDeletesAndBlocksProtectedOrReferencedValues()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var code = UniqueCode("ADM");

        var types = bll.AdminLookups.GetLookupTypes();
        var listBefore = await bll.AdminLookups.GetLookupItemsAsync(AdminLookupType.PropertyType);
        var created = await bll.AdminLookups.CreateLookupItemAsync(
            AdminLookupType.PropertyType,
            new AdminLookupEditDto
            {
                Code = code,
                Label = "Admin lookup"
            });
        var duplicate = await bll.AdminLookups.CreateLookupItemAsync(
            AdminLookupType.PropertyType,
            new AdminLookupEditDto
            {
                Code = code,
                Label = "Duplicate admin lookup"
            });
        var updated = await bll.AdminLookups.UpdateLookupItemAsync(
            AdminLookupType.PropertyType,
            created.Value.Id,
            new AdminLookupEditDto
            {
                Code = $"{code}_U",
                Label = "Admin lookup updated"
            });
        var deleteCheck = await bll.AdminLookups.GetDeleteCheckAsync(
            AdminLookupType.PropertyType,
            updated.Value.Id);
        var deleted = await bll.AdminLookups.DeleteLookupItemAsync(
            AdminLookupType.PropertyType,
            updated.Value.Id);
        var referencedCheck = await bll.AdminLookups.GetDeleteCheckAsync(
            AdminLookupType.PropertyType,
            TestTenants.PropertyTypeReferencedId);
        var referencedDelete = await bll.AdminLookups.DeleteLookupItemAsync(
            AdminLookupType.PropertyType,
            TestTenants.PropertyTypeReferencedId);
        var protectedUpdate = await bll.AdminLookups.UpdateLookupItemAsync(
            AdminLookupType.TicketStatus,
            TestTenants.TicketStatusCreatedId,
            new AdminLookupEditDto
            {
                Code = "CREATED_CHANGED",
                Label = "Created changed"
            });
        var listAfter = await bll.AdminLookups.GetLookupItemsAsync(AdminLookupType.PropertyType);

        types.Should().Contain(type => type.Type == AdminLookupType.PropertyType);
        listBefore.Items.Should().Contain(item => item.Id == TestTenants.PropertyTypeReferencedId);
        created.IsSuccess.Should().BeTrue();
        duplicate.ShouldFailWith<ConflictError>();
        updated.IsSuccess.Should().BeTrue();
        updated.Value.Code.Should().Be($"{code}_U");
        updated.Value.Label.Should().Be("Admin lookup updated");
        deleteCheck.BlockReason.Should().BeNull();
        deleted.IsSuccess.Should().BeTrue();
        referencedCheck.BlockReason.Should().NotBeNullOrWhiteSpace();
        referencedDelete.ShouldFailWith<BusinessRuleError>();
        protectedUpdate.ShouldFailWith<BusinessRuleError>();
        listAfter.Items.Should().NotContain(item => item.Id == updated.Value.Id);
    }

    private async Task<CompanySeed> CreateCompanyAsync(string suffix)
    {
        Guid userId;
        using (var userScope = _factory.Services.CreateScope())
        {
            var db = userScope.ServiceProvider.GetRequiredService<AppDbContext>();
            userId = (await CreateUserAsync(db, suffix)).Id;
        }

        using var companyScope = _factory.Services.CreateScope();
        var bll = companyScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var registryCode = UniqueCode("REG");
        var created = await bll.ManagementCompanies.CreateAsync(
            userId,
            new ManagementCompanyBllDto
            {
                Name = $"Admin Company {suffix}",
                RegistryCode = registryCode,
                VatNumber = "EE998877665",
                Email = $"{suffix}-{Guid.NewGuid():N}@test.ee",
                Phone = "+372 5555 8100",
                Address = "Admin Company Street 1"
            });

        created.IsSuccess.Should().BeTrue();
        return new CompanySeed(created.Value.Id, created.Value.Slug, created.Value.RegistryCode);
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
            FirstName = "Admin",
            LastName = suffix,
            CreatedAt = DateTime.UtcNow,
            LockoutEnabled = true
        };

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }

    private sealed record CompanySeed(Guid CompanyId, string Slug, string RegistryCode);
}
