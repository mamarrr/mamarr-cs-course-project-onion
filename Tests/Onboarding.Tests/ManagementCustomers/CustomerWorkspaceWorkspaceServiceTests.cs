using App.BLL.CustomerWorkspace.Workspace;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using Base.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Onboarding.Tests.ManagementCustomers;

public class CustomerWorkspaceWorkspaceServiceTests
{
    [Fact]
    public async Task AuthorizeAsync_ReturnsCompanyNotFound_WhenSlugDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var sut = new CustomerWorkspaceWorkspaceService(dbContext);

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

        var sut = new CustomerWorkspaceWorkspaceService(dbContext);
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

        var sut = new CustomerWorkspaceWorkspaceService(dbContext);
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

        var sut = new CustomerWorkspaceWorkspaceService(dbContext);
        var auth = await sut.AuthorizeAsync(appUser.Id, companyA.Slug);

        var createResult = await sut.CreateAsync(auth.Context!, new CustomerCreateRequest
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

        var sut = new CustomerWorkspaceWorkspaceService(dbContext);
        var auth = await sut.AuthorizeAsync(appUser.Id, company.Slug);

        var result = await sut.CreateAsync(auth.Context!, new CustomerCreateRequest
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

        var sut = new CustomerWorkspaceWorkspaceService(dbContext);
        var auth = await sut.AuthorizeAsync(appUser.Id, companyA.Slug);

        var result = await sut.CreateAsync(auth.Context!, new CustomerCreateRequest
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

        var sut = new CustomerWorkspaceWorkspaceService(dbContext);
        var auth = await sut.AuthorizeAsync(appUser.Id, company.Slug);

        var result = await sut.CreateAsync(auth.Context!, new CustomerCreateRequest
        {
            Name = "Äri Klient",
            RegistryCode = "REG-2"
        });

        Assert.True(result.Success);
        var created = await dbContext.Customers.SingleAsync(c => c.RegistryCode == "REG-2");
        Assert.Equal("ari-klient-2", created.Slug);
    }

    [Fact]
    public async Task AuthorizeCustomerContextAsync_ReturnsCustomerNotFound_WhenCustomerIsOutsideCompanyScope()
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
        dbContext.Customers.Add(CreateCustomer(companyB.Id, "South Customer", "S-1", "south-customer"));
        await dbContext.SaveChangesAsync();

        var sut = new CustomerWorkspaceWorkspaceService(dbContext);

        var result = await sut.AuthorizeCustomerContextAsync(appUser.Id, companyA.Slug, "south-customer");

        Assert.True(result.CustomerNotFound);
        Assert.False(result.IsAuthorized);
        Assert.False(result.IsForbidden);
    }

    [Fact]
    public async Task ListPropertiesAsync_ReturnsOnlyPropertiesFromAuthorizedCustomerScope()
    {
        await using var dbContext = CreateDbContext();
        var appUser = CreateUser("manager@test.local");
        var role = CreateRole("MANAGER");
        var company = CreateCompany("north-estate", "North Estate", "REG-NORTH");
        var customerA = CreateCustomer(company.Id, "Alpha", "A-1", "alpha");
        var customerB = CreateCustomer(company.Id, "Beta", "B-1", "beta");
        var propertyType = CreatePropertyType("APARTMENT_BUILDING", "Apartment building");

        dbContext.Users.Add(appUser);
        dbContext.ManagementCompanyRoles.Add(role);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(company.Id, appUser.Id, role.Id));
        dbContext.Customers.AddRange(customerA, customerB);
        dbContext.PropertyTypes.Add(propertyType);
        dbContext.Properties.AddRange(
            CreateProperty(customerA.Id, propertyType.Id, "Alpha House", "alpha-house"),
            CreateProperty(customerB.Id, propertyType.Id, "Beta House", "beta-house"));
        await dbContext.SaveChangesAsync();

        var sut = new CustomerWorkspaceWorkspaceService(dbContext);
        var access = await sut.AuthorizeCustomerContextAsync(appUser.Id, company.Slug, customerA.Slug);

        var result = await sut.ListPropertiesAsync(access.Context!);

        Assert.Single(result.Properties);
        Assert.Equal("alpha-house", result.Properties[0].PropertySlug);
    }

    [Fact]
    public async Task CreatePropertyAsync_CreatesPropertyOnlyInAuthorizedCustomerScope()
    {
        await using var dbContext = CreateDbContext();
        var appUser = CreateUser("manager@test.local");
        var role = CreateRole("MANAGER");
        var company = CreateCompany("north-estate", "North Estate", "REG-NORTH");
        var customerA = CreateCustomer(company.Id, "Alpha", "A-1", "alpha");
        var customerB = CreateCustomer(company.Id, "Beta", "B-1", "beta");
        var propertyType = CreatePropertyType("APARTMENT_BUILDING", "Apartment building");

        dbContext.Users.Add(appUser);
        dbContext.ManagementCompanyRoles.Add(role);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(company.Id, appUser.Id, role.Id));
        dbContext.Customers.AddRange(customerA, customerB);
        dbContext.PropertyTypes.Add(propertyType);
        await dbContext.SaveChangesAsync();

        var sut = new CustomerWorkspaceWorkspaceService(dbContext);
        var access = await sut.AuthorizeCustomerContextAsync(appUser.Id, company.Slug, customerA.Slug);

        var createResult = await sut.CreatePropertyAsync(access.Context!, new PropertyCreateRequest
        {
            Name = "Alpha House",
            AddressLine = "Main 1",
            City = "Tallinn",
            PostalCode = "10111",
            PropertyTypeId = propertyType.Id,
            Notes = "Primary building",
            IsActive = true
        });

        Assert.True(createResult.Success);
        var created = await dbContext.Properties.SingleAsync(p => p.Slug == "alpha-house");
        Assert.Equal(customerA.Id, created.CustomerId);
        Assert.NotEqual(customerB.Id, created.CustomerId);
    }

    [Fact]
    public async Task CreatePropertyAsync_GeneratesUniqueSlugWithinCustomerScope()
    {
        await using var dbContext = CreateDbContext();
        var appUser = CreateUser("manager@test.local");
        var role = CreateRole("MANAGER");
        var company = CreateCompany("north-estate", "North Estate", "REG-NORTH");
        var customerA = CreateCustomer(company.Id, "Alpha", "A-1", "alpha");
        var customerB = CreateCustomer(company.Id, "Beta", "B-1", "beta");
        var propertyType = CreatePropertyType("APARTMENT_BUILDING", "Apartment building");

        dbContext.Users.Add(appUser);
        dbContext.ManagementCompanyRoles.Add(role);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(company.Id, appUser.Id, role.Id));
        dbContext.Customers.AddRange(customerA, customerB);
        dbContext.PropertyTypes.Add(propertyType);
        dbContext.Properties.AddRange(
            CreateProperty(customerA.Id, propertyType.Id, "Äri Hoone", "ari-hoone"),
            CreateProperty(customerB.Id, propertyType.Id, "Äri Hoone", "ari-hoone"));
        await dbContext.SaveChangesAsync();

        var sut = new CustomerWorkspaceWorkspaceService(dbContext);
        var access = await sut.AuthorizeCustomerContextAsync(appUser.Id, company.Slug, customerA.Slug);

        var createResult = await sut.CreatePropertyAsync(access.Context!, new PropertyCreateRequest
        {
            Name = "Ari Hoone",
            AddressLine = "Main 2",
            City = "Tallinn",
            PostalCode = "10112",
            PropertyTypeId = propertyType.Id
        });

        Assert.True(createResult.Success);
        var created = await dbContext.Properties.SingleAsync(p => p.AddressLine == "Main 2");
        Assert.Equal("ari-hoone-2", created.Slug);
    }

    [Fact]
    public async Task ResolvePropertyDashboardContextAsync_ReturnsPropertyNotFound_WhenPropertyIsOutsideCustomerScope()
    {
        await using var dbContext = CreateDbContext();
        var appUser = CreateUser("manager@test.local");
        var role = CreateRole("MANAGER");
        var company = CreateCompany("north-estate", "North Estate", "REG-NORTH");
        var customerA = CreateCustomer(company.Id, "Alpha", "A-1", "alpha");
        var customerB = CreateCustomer(company.Id, "Beta", "B-1", "beta");
        var propertyType = CreatePropertyType("APARTMENT_BUILDING", "Apartment building");

        dbContext.Users.Add(appUser);
        dbContext.ManagementCompanyRoles.Add(role);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(company.Id, appUser.Id, role.Id));
        dbContext.Customers.AddRange(customerA, customerB);
        dbContext.PropertyTypes.Add(propertyType);
        dbContext.Properties.Add(CreateProperty(customerB.Id, propertyType.Id, "Beta House", "beta-house"));
        await dbContext.SaveChangesAsync();

        var sut = new CustomerWorkspaceWorkspaceService(dbContext);
        var access = await sut.AuthorizeCustomerContextAsync(appUser.Id, company.Slug, customerA.Slug);

        var result = await sut.ResolvePropertyDashboardContextAsync(access.Context!, "beta-house");

        Assert.True(result.PropertyNotFound);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public async Task ResolvePropertyDashboardContextAsync_ReturnsAuthorizedContext_WhenPropertyInScope()
    {
        await using var dbContext = CreateDbContext();
        var appUser = CreateUser("manager@test.local");
        var role = CreateRole("MANAGER");
        var company = CreateCompany("north-estate", "North Estate", "REG-NORTH");
        var customer = CreateCustomer(company.Id, "Alpha", "A-1", "alpha");
        var propertyType = CreatePropertyType("APARTMENT_BUILDING", "Apartment building");

        dbContext.Users.Add(appUser);
        dbContext.ManagementCompanyRoles.Add(role);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyUsers.Add(CreateMembership(company.Id, appUser.Id, role.Id));
        dbContext.Customers.Add(customer);
        dbContext.PropertyTypes.Add(propertyType);
        dbContext.Properties.Add(CreateProperty(customer.Id, propertyType.Id, "Alpha House", "alpha-house"));
        await dbContext.SaveChangesAsync();

        var sut = new CustomerWorkspaceWorkspaceService(dbContext);
        var access = await sut.AuthorizeCustomerContextAsync(appUser.Id, company.Slug, customer.Slug);

        var result = await sut.ResolvePropertyDashboardContextAsync(access.Context!, "alpha-house");

        Assert.True(result.IsAuthorized);
        Assert.NotNull(result.Context);
        Assert.Equal("alpha-house", result.Context!.PropertySlug);
        Assert.Equal(customer.Id, result.Context.CustomerId);
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

    private static PropertyType CreatePropertyType(string code, string label)
    {
        return new PropertyType
        {
            Id = Guid.NewGuid(),
            Code = code,
            Label = new LangStr(label)
        };
    }

    private static Property CreateProperty(Guid customerId, Guid propertyTypeId, string name, string slug)
    {
        return new Property
        {
            Id = Guid.NewGuid(),
            Label = new LangStr(name),
            Slug = slug,
            AddressLine = "Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CustomerId = customerId,
            PropertyTypeId = propertyTypeId
        };
    }
}

