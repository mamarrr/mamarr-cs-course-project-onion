using System.Security.Claims;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Profiles;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.PropertyWorkspace.Profiles;
using App.BLL.PropertyWorkspace.Properties;
using App.BLL.ResidentWorkspace.Access;
using App.BLL.ResidentWorkspace.Profiles;
using App.BLL.ResidentWorkspace.Residents;
using App.BLL.Shared.Profiles;
using App.BLL.UnitWorkspace.Access;
using App.BLL.UnitWorkspace.Profiles;
using App.BLL.UnitWorkspace.Workspace;
using App.DTO.v1.Customer;
using App.DTO.v1.Property;
using App.DTO.v1.Resident;
using App.DTO.v1.Unit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApp.ApiControllers.Customer;
using WebApp.ApiControllers.Property;
using WebApp.ApiControllers.Resident;
using WebApp.ApiControllers.Unit;
using Xunit;

namespace Onboarding.Tests;

public class ApiProfileSliceControllerTests
{
    [Fact]
    public async Task CustomerProfile_Get_ReturnsUnauthorized_WhenUserIdMissing()
    {
        var controller = CreateCustomerProfileController(BuildPrincipal(withNameIdentifier: false));

        var result = await controller.GetProfile("north-estate", "acme", CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task CustomerProfile_Get_ReturnsNotFound_WhenCustomerContextMissing()
    {
        var accessService = new Mock<ICustomerAccessService>();
        accessService
            .Setup(x => x.ResolveDashboardAccessAsync(It.IsAny<Guid>(), "north-estate", "missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerWorkspaceDashboardAccessResult { CustomerNotFound = true });

        var controller = CreateCustomerProfileController(BuildPrincipal(), accessService: accessService);

        var result = await controller.GetProfile("north-estate", "missing", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task CustomerProfile_Delete_ReturnsBadRequest_WhenConfirmationMismatch()
    {
        var context = BuildCustomerContext();
        var profileService = new Mock<IManagementCustomerProfileService>();
        profileService
            .Setup(x => x.GetProfileAsync(context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerProfileModel
            {
                CustomerId = context.CustomerId,
                CustomerSlug = context.CustomerSlug,
                Name = "Acme Customer",
                RegistryCode = "ACME-001",
                IsActive = true
            });

        var controller = CreateCustomerProfileController(
            BuildPrincipal(),
            accessContext: context,
            profileService: profileService);

        var result = await controller.DeleteProfile(
            context.CompanySlug,
            context.CustomerSlug,
            new DeleteCustomerProfileRequestDto { ConfirmationName = "Wrong Name" },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        profileService.Verify(x => x.DeleteProfileAsync(It.IsAny<CustomerWorkspaceDashboardContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CustomerProfile_Update_ReturnsBadRequest_WhenDuplicateRegistryCode()
    {
        var context = BuildCustomerContext();
        var profileService = new Mock<IManagementCustomerProfileService>();
        profileService
            .Setup(x => x.UpdateProfileAsync(context, It.IsAny<CustomerProfileUpdateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileOperationResult
            {
                DuplicateRegistryCode = true,
                ErrorMessage = "Customer with this registry code already exists in this company."
            });

        var controller = CreateCustomerProfileController(
            BuildPrincipal(),
            accessContext: context,
            profileService: profileService);

        var result = await controller.UpdateProfile(
            context.CompanySlug,
            context.CustomerSlug,
            new UpdateCustomerProfileRequestDto
            {
                Name = "Acme Customer",
                RegistryCode = "ACME-001",
                IsActive = true
            },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task PropertyProfile_Update_ReturnsOk_WhenSuccessful()
    {
        var context = BuildPropertyContext();
        var profileService = new Mock<IManagementPropertyProfileService>();
        profileService
            .Setup(x => x.UpdateProfileAsync(context, It.IsAny<PropertyProfileUpdateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileOperationResult { Success = true });
        profileService
            .Setup(x => x.GetProfileAsync(context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyProfileModel
            {
                PropertyId = context.PropertyId,
                PropertySlug = context.PropertySlug,
                Name = "Alpha House",
                AddressLine = "Main St 1",
                City = "Tallinn",
                PostalCode = "10111",
                IsActive = true
            });

        var controller = CreatePropertyProfileController(
            BuildPrincipal(),
            propertyContext: context,
            profileService: profileService);

        var result = await controller.UpdateProfile(
            context.CompanySlug,
            context.CustomerSlug,
            context.PropertySlug,
            new UpdatePropertyProfileRequestDto
            {
                Name = "Alpha House",
                AddressLine = "Main St 1",
                City = "Tallinn",
                PostalCode = "10111",
                IsActive = true
            },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    [Fact]
    public async Task UnitDashboard_Get_ReturnsNotFound_WhenUnitMissing()
    {
        var customerContext = BuildCustomerContext();
        var propertyContext = BuildPropertyContext(customerContext);

        var unitDashboardService = new Mock<IUnitAccessService>();
        unitDashboardService
            .Setup(x => x.ResolveUnitDashboardContextAsync(propertyContext, "u-404", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UnitDashboardAccessResult { UnitNotFound = true });

        var controller = CreateUnitDashboardController(
            BuildPrincipal(),
            customerContext: customerContext,
            propertyContext: propertyContext,
            unitDashboardService: unitDashboardService);

        var result = await controller.GetDashboard(
            customerContext.CompanySlug,
            customerContext.CustomerSlug,
            propertyContext.PropertySlug,
            "u-404",
            CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task UnitProfile_Delete_ReturnsNoContent_WhenSuccessful()
    {
        var unitContext = BuildUnitContext();
        var profileService = new Mock<IManagementUnitProfileService>();
        profileService
            .Setup(x => x.GetProfileAsync(unitContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UnitProfileModel
            {
                UnitId = unitContext.UnitId,
                UnitSlug = unitContext.UnitSlug,
                UnitNr = unitContext.UnitNr,
                IsActive = true
            });
        profileService
            .Setup(x => x.DeleteProfileAsync(unitContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileOperationResult { Success = true });

        var controller = CreateUnitProfileController(
            BuildPrincipal(),
            unitContext: unitContext,
            profileService: profileService);

        var result = await controller.DeleteProfile(
            unitContext.CompanySlug,
            unitContext.CustomerSlug,
            unitContext.PropertySlug,
            unitContext.UnitSlug,
            new DeleteUnitProfileRequestDto { ConfirmationUnitNr = unitContext.UnitNr },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ResidentDashboard_Get_ReturnsForbid_WhenForbidden()
    {
        var accessService = new Mock<IResidentAccessService>();
        accessService
            .Setup(x => x.ResolveDashboardAccessAsync(It.IsAny<Guid>(), "north-estate", "49001010001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResidentDashboardAccessResult { IsForbidden = true });

        var controller = CreateResidentDashboardController(BuildPrincipal(), accessService: accessService);

        var result = await controller.GetDashboard("north-estate", "49001010001", CancellationToken.None);

        var forbid = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbid.StatusCode);
    }

    [Fact]
    public async Task ResidentProfile_Update_ReturnsBadRequest_WhenDuplicateIdCode()
    {
        var context = BuildResidentContext();
        var profileService = new Mock<IManagementResidentProfileService>();
        profileService
            .Setup(x => x.UpdateProfileAsync(context, It.IsAny<ResidentProfileUpdateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileOperationResult
            {
                DuplicateIdCode = true,
                ErrorMessage = "Resident with this ID code already exists in this company."
            });

        var controller = CreateResidentProfileController(
            BuildPrincipal(),
            residentContext: context,
            profileService: profileService);

        var result = await controller.UpdateProfile(
            context.CompanySlug,
            context.ResidentIdCode,
            new UpdateResidentProfileRequestDto
            {
                FirstName = "Mari",
                LastName = "Tamm",
                IdCode = "49001010001",
                IsActive = true
            },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task ResidentProfile_Delete_ReturnsBadRequest_WhenConfirmationMismatch()
    {
        var context = BuildResidentContext();
        var profileService = new Mock<IManagementResidentProfileService>();
        profileService
            .Setup(x => x.GetProfileAsync(context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResidentProfileModel
            {
                ResidentId = context.ResidentId,
                ResidentIdCode = context.ResidentIdCode,
                FirstName = context.FirstName,
                LastName = context.LastName,
                IsActive = true
            });

        var controller = CreateResidentProfileController(
            BuildPrincipal(),
            residentContext: context,
            profileService: profileService);

        var result = await controller.DeleteProfile(
            context.CompanySlug,
            context.ResidentIdCode,
            new DeleteResidentProfileRequestDto { ConfirmationIdCode = "wrong" },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    private static CustomerProfileController CreateCustomerProfileController(
        ClaimsPrincipal user,
        Mock<ICustomerAccessService>? accessService = null,
        CustomerWorkspaceDashboardContext? accessContext = null,
        Mock<IManagementCustomerProfileService>? profileService = null)
    {
        accessContext ??= BuildCustomerContext();
        accessService ??= new Mock<ICustomerAccessService>();
        accessService
            .Setup(x => x.ResolveDashboardAccessAsync(It.IsAny<Guid>(), accessContext.CompanySlug, accessContext.CustomerSlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerWorkspaceDashboardAccessResult { IsAuthorized = true, Context = accessContext });

        profileService ??= new Mock<IManagementCustomerProfileService>();

        return AttachControllerContext(new CustomerProfileController(accessService.Object, profileService.Object), user);
    }

    private static PropertyProfileController CreatePropertyProfileController(
        ClaimsPrincipal user,
        CustomerWorkspaceDashboardContext? customerContext = null,
        PropertyDashboardContext? propertyContext = null,
        Mock<ICustomerAccessService>? customerAccessService = null,
        Mock<IPropertyWorkspaceService>? propertyAccessService = null,
        Mock<IManagementPropertyProfileService>? profileService = null)
    {
        customerContext ??= BuildCustomerContext();
        propertyContext ??= BuildPropertyContext(customerContext);
        customerAccessService ??= new Mock<ICustomerAccessService>();
        customerAccessService
            .Setup(x => x.ResolveDashboardAccessAsync(It.IsAny<Guid>(), customerContext.CompanySlug, customerContext.CustomerSlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerWorkspaceDashboardAccessResult { IsAuthorized = true, Context = customerContext });

        propertyAccessService ??= new Mock<IPropertyWorkspaceService>();
        propertyAccessService
            .Setup(x => x.ResolvePropertyDashboardContextAsync(customerContext, propertyContext.PropertySlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyDashboardAccessResult { IsAuthorized = true, Context = propertyContext });

        profileService ??= new Mock<IManagementPropertyProfileService>();

        return AttachControllerContext(new PropertyProfileController(customerAccessService.Object, propertyAccessService.Object, profileService.Object), user);
    }

    private static UnitDashboardController CreateUnitDashboardController(
        ClaimsPrincipal user,
        CustomerWorkspaceDashboardContext? customerContext = null,
        PropertyDashboardContext? propertyContext = null,
        Mock<ICustomerAccessService>? customerAccessService = null,
        Mock<IPropertyWorkspaceService>? propertyAccessService = null,
        Mock<IUnitAccessService>? unitDashboardService = null)
    {
        customerContext ??= BuildCustomerContext();
        propertyContext ??= BuildPropertyContext(customerContext);
        customerAccessService ??= new Mock<ICustomerAccessService>();
        customerAccessService
            .Setup(x => x.ResolveDashboardAccessAsync(It.IsAny<Guid>(), customerContext.CompanySlug, customerContext.CustomerSlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerWorkspaceDashboardAccessResult { IsAuthorized = true, Context = customerContext });

        propertyAccessService ??= new Mock<IPropertyWorkspaceService>();
        propertyAccessService
            .Setup(x => x.ResolvePropertyDashboardContextAsync(customerContext, propertyContext.PropertySlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyDashboardAccessResult { IsAuthorized = true, Context = propertyContext });

        unitDashboardService ??= new Mock<IUnitAccessService>();

        return AttachControllerContext(new UnitDashboardController(customerAccessService.Object, propertyAccessService.Object, unitDashboardService.Object), user);
    }

    private static UnitProfileController CreateUnitProfileController(
        ClaimsPrincipal user,
        CustomerWorkspaceDashboardContext? customerContext = null,
        PropertyDashboardContext? propertyContext = null,
        UnitDashboardContext? unitContext = null,
        Mock<ICustomerAccessService>? customerAccessService = null,
        Mock<IPropertyWorkspaceService>? propertyAccessService = null,
        Mock<IUnitAccessService>? unitDashboardService = null,
        Mock<IManagementUnitProfileService>? profileService = null)
    {
        unitContext ??= BuildUnitContext();
        customerContext ??= BuildCustomerContext(unitContext);
        propertyContext ??= BuildPropertyContext(customerContext, unitContext);
        customerAccessService ??= new Mock<ICustomerAccessService>();
        customerAccessService
            .Setup(x => x.ResolveDashboardAccessAsync(It.IsAny<Guid>(), customerContext.CompanySlug, customerContext.CustomerSlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerWorkspaceDashboardAccessResult { IsAuthorized = true, Context = customerContext });

        propertyAccessService ??= new Mock<IPropertyWorkspaceService>();
        propertyAccessService
            .Setup(x => x.ResolvePropertyDashboardContextAsync(customerContext, propertyContext.PropertySlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyDashboardAccessResult { IsAuthorized = true, Context = propertyContext });

        unitDashboardService ??= new Mock<IUnitAccessService>();
        unitDashboardService
            .Setup(x => x.ResolveUnitDashboardContextAsync(propertyContext, unitContext.UnitSlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UnitDashboardAccessResult { IsAuthorized = true, Context = unitContext });

        profileService ??= new Mock<IManagementUnitProfileService>();

        return AttachControllerContext(new UnitProfileController(
            customerAccessService.Object,
            propertyAccessService.Object,
            unitDashboardService.Object,
            profileService.Object), user);
    }

    private static ResidentDashboardController CreateResidentDashboardController(
        ClaimsPrincipal user,
        Mock<IResidentAccessService>? accessService = null,
        ResidentDashboardContext? residentContext = null)
    {
        residentContext ??= BuildResidentContext();
        accessService ??= new Mock<IResidentAccessService>();
        accessService
            .Setup(x => x.ResolveDashboardAccessAsync(It.IsAny<Guid>(), residentContext.CompanySlug, residentContext.ResidentIdCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResidentDashboardAccessResult { IsAuthorized = true, Context = residentContext });

        return AttachControllerContext(new ResidentDashboardController(accessService.Object), user);
    }

    private static ResidentProfileController CreateResidentProfileController(
        ClaimsPrincipal user,
        Mock<IResidentAccessService>? accessService = null,
        ResidentDashboardContext? residentContext = null,
        Mock<IManagementResidentProfileService>? profileService = null)
    {
        residentContext ??= BuildResidentContext();
        accessService ??= new Mock<IResidentAccessService>();
        accessService
            .Setup(x => x.ResolveDashboardAccessAsync(It.IsAny<Guid>(), residentContext.CompanySlug, residentContext.ResidentIdCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResidentDashboardAccessResult { IsAuthorized = true, Context = residentContext });

        profileService ??= new Mock<IManagementResidentProfileService>();

        return AttachControllerContext(new ResidentProfileController(accessService.Object, profileService.Object), user);
    }

    private static T AttachControllerContext<T>(T controller, ClaimsPrincipal user) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user
            }
        };

        return controller;
    }

    private static ClaimsPrincipal BuildPrincipal(bool withNameIdentifier = true)
    {
        var claims = new List<Claim>();
        if (withNameIdentifier)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
    }

    private static CustomerWorkspaceDashboardContext BuildCustomerContext(UnitDashboardContext? unitContext = null)
    {
        return new CustomerWorkspaceDashboardContext
        {
            AppUserId = unitContext?.AppUserId ?? Guid.NewGuid(),
            ManagementCompanyId = unitContext?.ManagementCompanyId ?? Guid.NewGuid(),
            CompanySlug = unitContext?.CompanySlug ?? "north-estate",
            CompanyName = unitContext?.CompanyName ?? "North Estate",
            CustomerId = unitContext?.CustomerId ?? Guid.NewGuid(),
            CustomerSlug = unitContext?.CustomerSlug ?? "acme",
            CustomerName = unitContext?.CustomerName ?? "Acme Customer"
        };
    }

    private static PropertyDashboardContext BuildPropertyContext(
        CustomerWorkspaceDashboardContext? customerContext = null,
        UnitDashboardContext? unitContext = null)
    {
        customerContext ??= BuildCustomerContext(unitContext);
        return new PropertyDashboardContext
        {
            AppUserId = customerContext.AppUserId,
            ManagementCompanyId = customerContext.ManagementCompanyId,
            CompanySlug = customerContext.CompanySlug,
            CompanyName = customerContext.CompanyName,
            CustomerId = customerContext.CustomerId,
            CustomerSlug = customerContext.CustomerSlug,
            CustomerName = customerContext.CustomerName,
            PropertyId = unitContext?.PropertyId ?? Guid.NewGuid(),
            PropertySlug = unitContext?.PropertySlug ?? "alpha-house",
            PropertyName = unitContext?.PropertyName ?? "Alpha House"
        };
    }

    private static UnitDashboardContext BuildUnitContext()
    {
        return new UnitDashboardContext
        {
            AppUserId = Guid.NewGuid(),
            ManagementCompanyId = Guid.NewGuid(),
            CompanySlug = "north-estate",
            CompanyName = "North Estate",
            CustomerId = Guid.NewGuid(),
            CustomerSlug = "acme",
            CustomerName = "Acme Customer",
            PropertyId = Guid.NewGuid(),
            PropertySlug = "alpha-house",
            PropertyName = "Alpha House",
            UnitId = Guid.NewGuid(),
            UnitSlug = "u-101",
            UnitNr = "101"
        };
    }

    private static ResidentDashboardContext BuildResidentContext()
    {
        return new ResidentDashboardContext
        {
            AppUserId = Guid.NewGuid(),
            ManagementCompanyId = Guid.NewGuid(),
            CompanySlug = "north-estate",
            CompanyName = "North Estate",
            ResidentId = Guid.NewGuid(),
            ResidentIdCode = "49001010001",
            FirstName = "Mari",
            LastName = "Tamm",
            FullName = "Mari Tamm",
            PreferredLanguage = "et",
            IsActive = true
        };
    }
}
