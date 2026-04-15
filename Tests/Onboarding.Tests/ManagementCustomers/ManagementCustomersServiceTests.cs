using App.BLL.ManagementCustomers;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using Base.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Onboarding.Tests.ManagementCustomers;

public class ManagementCustomersServiceTests
{
    [Fact]
    public async Task AuthorizeAsync_ReturnsCompanyNotFound_WhenSlugDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var sut = new ManagementCustomersService(dbContext);

        var result = await sut.AuthorizeAsync(Guid.NewGuid(), "missing-company");

        Assert.True(result.CompanyNotFound);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public async Task AuthorizeAsync_ReturnsForbidden_WhenMembershipRoleIsNotAllowed()
    {
        await using var dbContext = CreateDbContext();
        var appUser = CreateUser("resident@test.local");
        var company = CreateCompany("north-estate", "North Estate", "REG-NORTH");
        var residentRole = CreateRole("RESIDENT");

        dbContext.Users.Add(appUser);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyRoles.Add(residentRole);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(company.Id, appUser.Id, residentRole.Id));
        await dbContext.SaveChangesAsync();

        var sut = new ManagementCustomersService(dbContext);
        var result = await sut.AuthorizeAsync(appUser.Id, company.Slug);

        Assert.True(result.IsForbidden);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyCustomersFromAuthorizedTenant()
    {
        await using var dbContext = CreateDbContext();
        var appUser = CreateUser("manager@test.local");
        var role = CreateRole("MANAGER");
        var companyA = CreateCompany("north-estate", "North Estate", "REG-NORTH");
        var companyB = CreateCompany("south-estate", "South Estate", "REG-SOUTH");

        dbContext.Users.Add(appUser);
        dbContext.ManagementCompanyRoles.Add(role);
        dbContext.ManagementCompanies.AddRange(companyA, companyB);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(companyA.Id, appUser.Id, role.Id));
        dbContext.Customers.AddRange(
            CreateCustomer(companyA.Id, "Acme A", "CUST-A", "acme-a"),
            CreateCustomer(companyB.Id, "Acme B", "CUST-B", "acme-b"));
        await dbContext.SaveChangesAsync();

        var sut = new ManagementCustomersService(dbContext);
        var auth = await sut.AuthorizeAsync(appUser.Id, companyA.Slug);

        var result = await sut.ListAsync(auth.Context!);

        Assert.Single(result.Customers);
        Assert.Equal("Acme A", result.Customers[0].Name);
    }

    [Fact]
    public async Task CreateAsync_CreatesCustomerOnlyInAuthorizedTenant()
    {
        await using var dbContext = CreateDbContext();
        var appUser = CreateUser("owner@test.local");
        var role = CreateRole("OWNER");
        var companyA = CreateCompany("north-estate", "North Estate", "REG-NORTH");
        var companyB = CreateCompany("south-estate", "South Estate", "REG-SOUTH");

        dbContext.Users.Add(appUser);
        dbContext.ManagementCompanyRoles.Add(role);
        dbContext.ManagementCompanies.AddRange(companyA, companyB);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(companyA.Id, appUser.Id, role.Id));
        await dbContext.SaveChangesAsync();

        var sut = new ManagementCustomersService(dbContext);
        var auth = await sut.AuthorizeAsync(appUser.Id, companyA.Slug);

        var createResult = await sut.CreateAsync(auth.Context!, new ManagementCustomerCreateRequest
        {
            Name = "Tenant A Customer",
            RegistryCode = "CUST-A-100",
            BillingEmail = "billing@a.test",
            BillingAddress = "Street 1",
            Phone = "+372100000"
        });

        Assert.True(createResult.Success);

        var created = await dbContext.Customers.SingleAsync(c => c.RegistryCode == "CUST-A-100");
        Assert.Equal(companyA.Id, created.ManagementCompanyId);
        Assert.NotEqual(companyB.Id, created.ManagementCompanyId);
    }

    [Fact]
    public async Task CreateAsync_BlocksDuplicateRegistryCode_InsideSameTenant()
    {
        await using var dbContext = CreateDbContext();
        var appUser = CreateUser("manager@test.local");
        var role = CreateRole("MANAGER");
        var company = CreateCompany("north-estate", "North Estate", "REG-NORTH");

        dbContext.Users.Add(appUser);
        dbContext.ManagementCompanyRoles.Add(role);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(company.Id, appUser.Id, role.Id));
        dbContext.Customers.Add(CreateCustomer(company.Id, "Acme", "DUP-REG", "acme"));
        await dbContext.SaveChangesAsync();

        var sut = new ManagementCustomersService(dbContext);
        var auth = await sut.AuthorizeAsync(appUser.Id, company.Slug);

        var result = await sut.CreateAsync(auth.Context!, new ManagementCustomerCreateRequest
        {
            Name = "Another Acme",
            RegistryCode = "dup-reg"
        });

        Assert.False(result.Success);
        Assert.True(result.DuplicateRegistryCode);
    }

    [Fact]
    public async Task CreateAsync_AllowsSameRegistryCode_AcrossDifferentTenants()
    {
        await using var dbContext = CreateDbContext();
        var appUser = CreateUser("owner@test.local");
        var role = CreateRole("OWNER");
        var companyA = CreateCompany("north-estate", "North Estate", "REG-NORTH");
        var companyB = CreateCompany("south-estate", "South Estate", "REG-SOUTH");

        dbContext.Users.Add(appUser);
        dbContext.ManagementCompanyRoles.Add(role);
        dbContext.ManagementCompanies.AddRange(companyA, companyB);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(companyA.Id, appUser.Id, role.Id));
        dbContext.Customers.Add(CreateCustomer(companyB.Id, "South Existing", "SAME-REG", "south-existing"));
        await dbContext.SaveChangesAsync();

        var sut = new ManagementCustomersService(dbContext);
        var auth = await sut.AuthorizeAsync(appUser.Id, companyA.Slug);

        var result = await sut.CreateAsync(auth.Context!, new ManagementCustomerCreateRequest
        {
            Name = "North New",
            RegistryCode = "SAME-REG"
        });

        Assert.True(result.Success);
        Assert.Equal(2, await dbContext.Customers.CountAsync(c => c.RegistryCode == "SAME-REG"));
    }

    [Fact]
    public async Task CreateAsync_GeneratesUniqueSlug_WhenBaseSlugAlreadyExistsInTenant()
    {
        await using var dbContext = CreateDbContext();
        var appUser = CreateUser("owner@test.local");
        var role = CreateRole("OWNER");
        var company = CreateCompany("north-estate", "North Estate", "REG-NORTH");

        dbContext.Users.Add(appUser);
        dbContext.ManagementCompanyRoles.Add(role);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(company.Id, appUser.Id, role.Id));
        dbContext.Customers.Add(CreateCustomer(company.Id, "Ari Klient", "REG-1", "ari-klient"));
        await dbContext.SaveChangesAsync();

        var sut = new ManagementCustomersService(dbContext);
        var auth = await sut.AuthorizeAsync(appUser.Id, company.Slug);

        var result = await sut.CreateAsync(auth.Context!, new ManagementCustomerCreateRequest
        {
            Name = "Äri Klient",
            RegistryCode = "REG-2"
        });

        Assert.True(result.Success);
        var created = await dbContext.Customers.SingleAsync(c => c.RegistryCode == "REG-2");
        Assert.Equal("ari-klient-2", created.Slug);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AppUser CreateUser(string email)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
    }

    private static ManagementCompany CreateCompany(string slug, string name, string registryCode)
    {
        return new ManagementCompany
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = name,
            RegistryCode = registryCode,
            VatNumber = $"VAT-{registryCode}",
            Email = $"{slug}@test.local",
            Phone = "+3720000000",
            Address = "Address 1",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
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

    private static ManagementCompanyUser CreateMembership(Guid companyId, Guid appUserId, Guid roleId)
    {
        return new ManagementCompanyUser
        {
            Id = Guid.NewGuid(),
            ManagementCompanyId = companyId,
            AppUserId = appUserId,
            ManagementCompanyRoleId = roleId,
            JobTitle = new LangStr("Member"),
            IsActive = true,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            ValidTo = null,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Customer CreateCustomer(Guid companyId, string name, string registryCode, string slug)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            Name = name,
            RegistryCode = registryCode,
            Slug = slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ManagementCompanyId = companyId
        };
    }
}
