using System.Net;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.UnitWorkspace.Workspace;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using App.DTO.v1.Customer;
using App.DTO.v1.Management;
using App.DTO.v1.Property;
using Base.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebApp.ApiControllers.Customer;
using WebApp.ApiControllers.Management;
using WebApp.ApiControllers.Property;
using Xunit;
using CustomerDashboardControllerApi = WebApp.ApiControllers.Customer.CustomerDashboardController;
using PropertyDashboardControllerApi = WebApp.ApiControllers.Property.PropertyDashboardController;
using PropertyUnitsControllerApi = WebApp.ApiControllers.Property.PropertyUnitsController;

namespace Onboarding.Tests;

public class ApiManagementCreationChainControllerTests
{
    [Fact]
    public async Task ManagementCustomers_Get_ReturnsUnauthorized_WhenUserMissing()
    {
        await using var dbContext = CreateDbContext(nameof(ManagementCustomers_Get_ReturnsUnauthorized_WhenUserMissing));
        SeedPropertyType(dbContext);
        var service = CreateManagementCustomersService(dbContext);
        var controller = CreateManagementCustomersController(service, dbContext, user: null);

        var result = await controller.GetCustomers("north-estate", CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal((int)HttpStatusCode.Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task ManagementCustomers_Create_ReturnsCreated_AndListsCreatedCustomer()
    {
        await using var dbContext = CreateDbContext(nameof(ManagementCustomers_Create_ReturnsCreated_AndListsCreatedCustomer));
        SeedManagementMembership(dbContext, TestIds.User1Id, TestIds.Company1Id, "north-estate", "North Estate");
        SeedPropertyType(dbContext);
        var service = CreateManagementCustomersService(dbContext);
        var controller = CreateManagementCustomersController(service, dbContext, TestIds.User1Id);

        var createResult = await controller.CreateCustomer("north-estate", new CreateManagementCustomerRequestDto
        {
            Name = "Acme Customer",
            RegistryCode = "ACME-001",
            BillingEmail = "billing@acme.test",
            BillingAddress = "Main street 1",
            Phone = "+37255550000"
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdDto = Assert.IsType<CreateManagementCustomerResponseDto>(created.Value);
        Assert.Equal("customer-dashboard", createdDto.RouteContext.CurrentSection);
        Assert.False(Guid.Empty == createdDto.CustomerId);

        var listResult = await controller.GetCustomers("north-estate", CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(listResult.Result);
        var listDto = Assert.IsType<ManagementCustomersResponseDto>(ok.Value);
        var customer = Assert.Single(listDto.Customers);
        Assert.Equal(createdDto.CustomerId, customer.CustomerId);
        Assert.Equal(createdDto.CustomerSlug, customer.CustomerSlug);
    }

    [Fact]
    public async Task ManagementCustomers_Create_ReturnsBadRequest_ForDuplicateRegistryCode()
    {
        await using var dbContext = CreateDbContext(nameof(ManagementCustomers_Create_ReturnsBadRequest_ForDuplicateRegistryCode));
        SeedManagementMembership(dbContext, TestIds.User1Id, TestIds.Company1Id, "north-estate", "North Estate");
        SeedCustomer(dbContext, TestIds.Company1Id, "acme", "Acme", "ACME-001");
        SeedPropertyType(dbContext);
        var service = CreateManagementCustomersService(dbContext);
        var controller = CreateManagementCustomersController(service, dbContext, TestIds.User1Id);

        var result = await controller.CreateCustomer("north-estate", new CreateManagementCustomerRequestDto
        {
            Name = "Another Acme",
            RegistryCode = "ACME-001"
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal((int)HttpStatusCode.BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task CustomerDashboard_Get_ReturnsNotFound_ForCrossTenantCustomer()
    {
        await using var dbContext = CreateDbContext(nameof(CustomerDashboard_Get_ReturnsNotFound_ForCrossTenantCustomer));
        SeedManagementMembership(dbContext, TestIds.User1Id, TestIds.Company1Id, "north-estate", "North Estate");
        SeedManagementMembership(dbContext, TestIds.User2Id, TestIds.Company2Id, "south-estate", "South Estate");
        SeedCustomer(dbContext, TestIds.Company2Id, "other-customer", "Other Customer", "OC-001");
        var service = CreateManagementCustomersService(dbContext);
        var controller = CreateCustomerDashboardController(service, dbContext, TestIds.User1Id);

        var result = await controller.GetDashboard("north-estate", "other-customer", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal((int)HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task CustomerProperties_Create_And_List_ReturnPropertyTypeOptionsAndCreatedProperty()
    {
        await using var dbContext = CreateDbContext(nameof(CustomerProperties_Create_And_List_ReturnPropertyTypeOptionsAndCreatedProperty));
        SeedManagementMembership(dbContext, TestIds.User1Id, TestIds.Company1Id, "north-estate", "North Estate");
        var propertyTypeId = SeedPropertyType(dbContext);
        SeedCustomer(dbContext, TestIds.Company1Id, "acme", "Acme", "ACME-001");
        var service = CreateManagementCustomersService(dbContext);
        var controller = CreateCustomerPropertiesController(service, dbContext, TestIds.User1Id);

        var createResult = await controller.CreateProperty("north-estate", "acme", new CreateCustomerPropertyRequestDto
        {
            Name = "Alpha House",
            AddressLine = "Main 1",
            City = "Tallinn",
            PostalCode = "10111",
            PropertyTypeId = propertyTypeId,
            IsActive = true
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdDto = Assert.IsType<CreateCustomerPropertyResponseDto>(created.Value);
        Assert.Equal("property-dashboard", createdDto.RouteContext.CurrentSection);

        var listResult = await controller.GetProperties("north-estate", "acme", CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(listResult.Result);
        var listDto = Assert.IsType<CustomerPropertiesResponseDto>(ok.Value);
        Assert.Single(listDto.Properties);
        Assert.Single(listDto.PropertyTypeOptions);
        Assert.Equal(createdDto.PropertyId, listDto.Properties[0].PropertyId);
    }

    [Fact]
    public async Task CustomerProperties_Create_ReturnsBadRequest_WhenPropertyTypeInvalid()
    {
        await using var dbContext = CreateDbContext(nameof(CustomerProperties_Create_ReturnsBadRequest_WhenPropertyTypeInvalid));
        SeedManagementMembership(dbContext, TestIds.User1Id, TestIds.Company1Id, "north-estate", "North Estate");
        SeedPropertyType(dbContext);
        SeedCustomer(dbContext, TestIds.Company1Id, "acme", "Acme", "ACME-001");
        var service = CreateManagementCustomersService(dbContext);
        var controller = CreateCustomerPropertiesController(service, dbContext, TestIds.User1Id);

        var result = await controller.CreateProperty("north-estate", "acme", new CreateCustomerPropertyRequestDto
        {
            Name = "Alpha House",
            AddressLine = "Main 1",
            City = "Tallinn",
            PostalCode = "10111",
            PropertyTypeId = Guid.NewGuid()
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal((int)HttpStatusCode.BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task PropertyDashboard_Get_ReturnsNotFound_WhenPropertyNotInCustomerChain()
    {
        await using var dbContext = CreateDbContext(nameof(PropertyDashboard_Get_ReturnsNotFound_WhenPropertyNotInCustomerChain));
        SeedManagementMembership(dbContext, TestIds.User1Id, TestIds.Company1Id, "north-estate", "North Estate");
        var propertyTypeId = SeedPropertyType(dbContext);
        var customer1Id = SeedCustomer(dbContext, TestIds.Company1Id, "acme", "Acme", "ACME-001");
        var customer2Id = SeedCustomer(dbContext, TestIds.Company1Id, "beta", "Beta", "BETA-001");
        SeedProperty(dbContext, customer2Id, propertyTypeId, "beta-house", "Beta House");
        var service = CreateManagementCustomersService(dbContext);
        var controller = CreatePropertyDashboardController(service, dbContext, TestIds.User1Id);

        var result = await controller.GetDashboard("north-estate", "acme", "beta-house", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal((int)HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task PropertyUnits_Create_And_List_ReturnCreatedUnit()
    {
        await using var dbContext = CreateDbContext(nameof(PropertyUnits_Create_And_List_ReturnCreatedUnit));
        SeedManagementMembership(dbContext, TestIds.User1Id, TestIds.Company1Id, "north-estate", "North Estate");
        var propertyTypeId = SeedPropertyType(dbContext);
        var customerId = SeedCustomer(dbContext, TestIds.Company1Id, "acme", "Acme", "ACME-001");
        SeedProperty(dbContext, customerId, propertyTypeId, "alpha-house", "Alpha House");
        var service = CreateManagementCustomersService(dbContext);
        var controller = CreatePropertyUnitsController(service, dbContext, TestIds.User1Id);

        var createResult = await controller.CreateUnit("north-estate", "acme", "alpha-house", new CreatePropertyUnitRequestDto
        {
            UnitNr = "12",
            FloorNr = 3,
            SizeM2 = 55.5m
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdDto = Assert.IsType<CreatePropertyUnitResponseDto>(created.Value);
        Assert.Equal("property-units", createdDto.RouteContext.CurrentSection);

        var listResult = await controller.GetUnits("north-estate", "acme", "alpha-house", CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(listResult.Result);
        var listDto = Assert.IsType<PropertyUnitsResponseDto>(ok.Value);
        var unit = Assert.Single(listDto.Units);
        Assert.Equal(createdDto.UnitId, unit.UnitId);
        Assert.Equal("12", unit.UnitNr);
    }

    [Fact]
    public async Task PropertyUnits_Create_ReturnsBadRequest_WhenUnitNrInvalid()
    {
        await using var dbContext = CreateDbContext(nameof(PropertyUnits_Create_ReturnsBadRequest_WhenUnitNrInvalid));
        SeedManagementMembership(dbContext, TestIds.User1Id, TestIds.Company1Id, "north-estate", "North Estate");
        var propertyTypeId = SeedPropertyType(dbContext);
        var customerId = SeedCustomer(dbContext, TestIds.Company1Id, "acme", "Acme", "ACME-001");
        SeedProperty(dbContext, customerId, propertyTypeId, "alpha-house", "Alpha House");
        var service = CreateManagementCustomersService(dbContext);
        var controller = CreatePropertyUnitsController(service, dbContext, TestIds.User1Id);

        var result = await controller.CreateUnit("north-estate", "acme", "alpha-house", new CreatePropertyUnitRequestDto
        {
            UnitNr = "   "
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal((int)HttpStatusCode.BadRequest, badRequest.StatusCode);
    }

    private static CustomersController CreateManagementCustomersController(CustomerWorkspaceWorkspaceService workspaceService, AppDbContext dbContext, Guid? user)
    {
        return new CustomersController(workspaceService, workspaceService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext(user)
            }
        };
    }

    private static CustomerDashboardControllerApi CreateCustomerDashboardController(CustomerWorkspaceWorkspaceService workspaceService, AppDbContext dbContext, Guid? user)
    {
        return new CustomerDashboardControllerApi(workspaceService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext(user)
            }
        };
    }

    private static CustomerPropertiesController CreateCustomerPropertiesController(CustomerWorkspaceWorkspaceService workspaceService, AppDbContext dbContext, Guid? user)
    {
        return new CustomerPropertiesController(workspaceService, workspaceService, dbContext)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext(user)
            }
        };
    }

    private static PropertyDashboardControllerApi CreatePropertyDashboardController(CustomerWorkspaceWorkspaceService workspaceService, AppDbContext dbContext, Guid? user)
    {
        return new PropertyDashboardControllerApi(workspaceService, workspaceService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext(user)
            }
        };
    }

    private static PropertyUnitsControllerApi CreatePropertyUnitsController(CustomerWorkspaceWorkspaceService workspaceService, AppDbContext dbContext, Guid? user)
    {
        var unitService = new UnitWorkspaceService(dbContext);
        return new PropertyUnitsControllerApi(workspaceService, workspaceService, unitService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext(user)
            }
        };
    }

    private static DefaultHttpContext CreateHttpContext(Guid? userId)
    {
        var httpContext = new DefaultHttpContext();
        if (userId != null)
        {
            httpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.Value.ToString())],
                    "TestAuthType"));
        }

        return httpContext;
    }

    private static CustomerWorkspaceWorkspaceService CreateManagementCustomersService(AppDbContext dbContext)
    {
        return new CustomerWorkspaceWorkspaceService(dbContext);
    }

    private static AppDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new AppDbContext(options);
    }

    private static Guid SeedPropertyType(AppDbContext dbContext)
    {
        var id = Guid.NewGuid();
        dbContext.PropertyTypes.Add(new PropertyType
        {
            Id = id,
            Code = "APARTMENT_BUILDING",
            Label = new LangStr("Apartment building")
        });
        dbContext.SaveChanges();
        return id;
    }

    private static void SeedManagementMembership(AppDbContext dbContext, Guid userId, Guid companyId, string slug, string name)
    {
        var roleId = Guid.NewGuid();
        dbContext.ManagementCompanyRoles.Add(new ManagementCompanyRole
        {
            Id = roleId,
            Code = $"OWNER-{slug}",
            Label = "Owner"
        });

        dbContext.ManagementCompanies.Add(new ManagementCompany
        {
            Id = companyId,
            Name = name,
            Slug = slug,
            RegistryCode = $"REG-{slug}",
            VatNumber = $"VAT-{slug}",
            Email = $"{slug}@example.com",
            Phone = "+37200000000",
            Address = "Address",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });

        dbContext.ManagementCompanyUsers.Add(new ManagementCompanyUser
        {
            Id = Guid.NewGuid(),
            AppUserId = userId,
            ManagementCompanyId = companyId,
            ManagementCompanyRoleId = roleId,
            JobTitle = "Owner",
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();
    }

    private static Guid SeedCustomer(AppDbContext dbContext, Guid companyId, string slug, string name, string registryCode)
    {
        var id = Guid.NewGuid();
        dbContext.Customers.Add(new Customer
        {
            Id = id,
            ManagementCompanyId = companyId,
            Name = name,
            RegistryCode = registryCode,
            Slug = slug,
            BillingEmail = $"{slug}@billing.test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        dbContext.SaveChanges();
        return id;
    }

    private static Guid SeedProperty(AppDbContext dbContext, Guid customerId, Guid propertyTypeId, string slug, string name)
    {
        var id = Guid.NewGuid();
        dbContext.Properties.Add(new Property
        {
            Id = id,
            CustomerId = customerId,
            Label = new LangStr(name),
            AddressLine = "Main 1",
            City = "Tallinn",
            PostalCode = "10111",
            PropertyTypeId = propertyTypeId,
            Slug = slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        dbContext.SaveChanges();
        return id;
    }

    private static class TestIds
    {
        public static readonly Guid User1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid User2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid Company1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static readonly Guid Company2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    }
}
