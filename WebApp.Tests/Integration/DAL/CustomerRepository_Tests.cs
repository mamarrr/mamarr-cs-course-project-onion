using App.DAL.Contracts;
using App.DAL.DTO.Customers;
using App.DAL.EF;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.DAL;

public class CustomerRepository_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CustomerRepository_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AllByCompanySlugAsync_ReturnsSeededCustomersForCompany()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var customers = await uow.Customers.AllByCompanySlugAsync($" {TestTenants.CompanyASlug} ");

        customers.Should().ContainSingle(customer => customer.Id == TestTenants.CustomerAId);
        customers.Should().OnlyContain(customer => customer.ManagementCompanyId == TestTenants.CompanyAId);
        customers.Select(customer => customer.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task AllByCompanyIdAsync_ReturnsTenantScopedCustomers()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var customers = await uow.Customers.AllByCompanyIdAsync(TestTenants.CompanyAId);
        var missing = await uow.Customers.AllByCompanyIdAsync(Guid.NewGuid());

        customers.Should().ContainSingle(customer => customer.Id == TestTenants.CustomerAId);
        customers.Should().OnlyContain(customer => customer.ManagementCompanyId == TestTenants.CompanyAId);
        missing.Should().BeEmpty();
    }

    [Fact]
    public async Task FirstWorkspaceByCompanyAndSlugAsync_ReturnsCompanyAndCustomerContext()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var workspace = await uow.Customers.FirstWorkspaceByCompanyAndSlugAsync(
            TestTenants.CompanyAId,
            " customer-a ");

        workspace.Should().NotBeNull();
        workspace!.Id.Should().Be(TestTenants.CustomerAId);
        workspace.ManagementCompanyId.Should().Be(TestTenants.CompanyAId);
        workspace.CompanySlug.Should().Be(TestTenants.CompanyASlug);
        workspace.CompanyName.Should().Be(TestTenants.CompanyAName);
        workspace.Slug.Should().Be("customer-a");
    }

    [Fact]
    public async Task ProfileQueries_ReturnCustomerOnlyWithinCompanyContext()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var bySlugs = await uow.Customers.FirstProfileByCompanyAndSlugAsync(
            $" {TestTenants.CompanyASlug} ",
            " customer-a ");
        var byIds = await uow.Customers.FindProfileAsync(TestTenants.CustomerAId, TestTenants.CompanyAId);
        var crossTenant = await uow.Customers.FindProfileAsync(TestTenants.CustomerAId, Guid.NewGuid());

        bySlugs.Should().NotBeNull();
        byIds.Should().NotBeNull();
        bySlugs!.Id.Should().Be(TestTenants.CustomerAId);
        byIds!.Slug.Should().Be("customer-a");
        bySlugs.CompanySlug.Should().Be(TestTenants.CompanyASlug);
        bySlugs.RegistryCode.Should().Be("TEST-CUSTOMER-A");
        crossTenant.Should().BeNull();
    }

    [Fact]
    public async Task ExistenceAndUniquenessQueries_AreTenantScoped()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var slugExists = await uow.Customers.CustomerSlugExistsInCompanyAsync(TestTenants.CompanyAId, " CUSTOMER-A ");
        var registryExists = await uow.Customers.RegistryCodeExistsInCompanyAsync(TestTenants.CompanyAId, " test-customer-a ");
        var registryExceptSelf = await uow.Customers.RegistryCodeExistsInCompanyAsync(
            TestTenants.CompanyAId,
            "TEST-CUSTOMER-A",
            TestTenants.CustomerAId);
        var existsInCompany = await uow.Customers.ExistsInCompanyAsync(TestTenants.CustomerAId, TestTenants.CompanyAId);
        var existsInOtherCompany = await uow.Customers.ExistsInCompanyAsync(TestTenants.CustomerAId, Guid.NewGuid());

        slugExists.Should().BeTrue();
        registryExists.Should().BeTrue();
        registryExceptSelf.Should().BeFalse();
        existsInCompany.Should().BeTrue();
        existsInOtherCompany.Should().BeFalse();
    }

    [Fact]
    public async Task OptionsForTicketAsync_ReturnsCustomerOptionsForCompany()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var options = await uow.Customers.OptionsForTicketAsync(TestTenants.CompanyAId);

        options.Should().ContainSingle(option => option.Id == TestTenants.CustomerAId && option.Label == "Customer A");
        options.Select(option => option.Label).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task FindActiveManagementCompanyRoleCodeAsync_UsesActiveMembership()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var roleCode = await uow.Customers.FindActiveManagementCompanyRoleCodeAsync(
            TestTenants.CompanyAId,
            TestUsers.CompanyAOwnerId);
        var missingRole = await uow.Customers.FindActiveManagementCompanyRoleCodeAsync(
            TestTenants.CompanyAId,
            TestUsers.SystemAdminId);

        roleCode.Should().Be("OWNER");
        missingRole.Should().BeNull();
    }

    [Fact]
    public async Task HasDeleteDependenciesAsync_DetectsSeededCustomerDependencies()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var hasDependencies = await uow.Customers.HasDeleteDependenciesAsync(
            TestTenants.CustomerAId,
            TestTenants.CompanyAId);
        var wrongCompany = await uow.Customers.HasDeleteDependenciesAsync(
            TestTenants.CustomerAId,
            Guid.NewGuid());

        hasDependencies.Should().BeTrue();
        wrongCompany.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCurrentCultureNotesAndPreservesOtherTranslations()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var customerId = Guid.NewGuid();
        var slug = UniqueSlug("customer-update");

        uow.Customers.Add(new CustomerDalDto
        {
            Id = customerId,
            ManagementCompanyId = TestTenants.CompanyAId,
            Name = "Update Customer",
            Slug = slug,
            RegistryCode = UniqueCode("CUST"),
            BillingEmail = "update-customer@test.ee",
            BillingAddress = "Update Street 1",
            Phone = "+372 5555 3001",
            Notes = "English note"
        });
        await uow.SaveChangesAsync(CancellationToken.None);

        var entity = await db.Customers.AsTracking().SingleAsync(customer => customer.Id == customerId);
        entity.Notes!.SetTranslation("Eesti märkus", "et");
        db.Entry(entity).Property(customer => customer.Notes).IsModified = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await uow.Customers.UpdateAsync(new CustomerDalDto
        {
            Id = customerId,
            ManagementCompanyId = TestTenants.CompanyAId,
            Name = "Updated Customer",
            Slug = slug,
            RegistryCode = UniqueCode("CUST"),
            BillingEmail = "updated-customer@test.ee",
            BillingAddress = "Updated Street 1",
            Phone = "+372 5555 3002",
            Notes = "English updated"
        }, TestTenants.CompanyAId);
        await uow.SaveChangesAsync(CancellationToken.None);

        var persisted = await db.Customers.AsNoTracking().SingleAsync(customer => customer.Id == customerId);
        persisted.Name.Should().Be("Updated Customer");
        persisted.Notes!.Translate("en").Should().Be("English updated");
        persisted.Notes.Translate("et").Should().Be("Eesti märkus");
    }

    private static string UniqueSlug(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32];
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }
}
