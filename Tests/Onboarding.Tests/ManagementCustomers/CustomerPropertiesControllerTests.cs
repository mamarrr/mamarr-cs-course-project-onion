using System.Security.Claims;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.PropertyWorkspace.Properties;
using App.DAL.EF;
using App.Domain;
using Base.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Areas.Customer.Controllers;
using WebApp.UI.Chrome;
using WebApp.ViewModels.Customer.CustomerProperties;
using Xunit;

namespace Onboarding.Tests.ManagementCustomers;

public class CustomerPropertiesControllerTests
{
    [Fact]
    public async Task Index_ReturnsChallenge_WhenUserIdClaimMissing()
    {
        var serviceMock = new Mock<ICustomerWorkspaceService>();
        var controller = CreateController(serviceMock.Object, BuildPrincipal(withNameIdentifier: false));

        var result = await controller.Index("north-estate", "acme", CancellationToken.None);

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsNotFound_WhenCustomerScopeMissing()
    {
        var serviceMock = new Mock<ICustomerWorkspaceService>();
        serviceMock
            .Setup(x => x.ResolveDashboardAccessAsync(It.IsAny<Guid>(), "north-estate", "acme", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerDashboardAccessResult
            {
                CompanyNotFound = true
            });

        var controller = CreateController(serviceMock.Object, BuildPrincipal());

        var result = await controller.Index("north-estate", "acme", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsForbid_WhenForbidden()
    {
        var serviceMock = new Mock<ICustomerWorkspaceService>();
        serviceMock
            .Setup(x => x.ResolveDashboardAccessAsync(It.IsAny<Guid>(), "north-estate", "acme", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerDashboardAccessResult
            {
                IsForbidden = true
            });

        var controller = CreateController(serviceMock.Object, BuildPrincipal());

        var result = await controller.Index("north-estate", "acme", CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Add_InvalidModel_ReturnsIndexView()
    {
        var context = BuildDashboardContext();
        var serviceMock = BuildAuthorizedService(context);
        serviceMock
            .Setup(x => x.ListPropertiesAsync(context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerPropertyListResult());

        var controller = CreateController(serviceMock.Object, BuildPrincipal());
        controller.ModelState.AddModelError("AddProperty.Name", "required");

        var vm = new PropertiesPageViewModel
        {
            AddProperty = new AddPropertyViewModel()
        };

        var result = await controller.Add("north-estate", "acme", vm, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.IsType<PropertiesPageViewModel>(view.Model);
    }

    [Fact]
    public async Task Add_Success_RedirectsToIndex_AndSetsTempData()
    {
        var context = BuildDashboardContext();
        var serviceMock = BuildAuthorizedService(context);

        serviceMock
            .Setup(x => x.CreatePropertyAsync(
                context,
                It.IsAny<PropertyCreateRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyCreateResult
            {
                Success = true,
                CreatedPropertyId = Guid.NewGuid(),
                CreatedPropertySlug = "alpha-house"
            });

        var controller = CreateController(serviceMock.Object, BuildPrincipal());
        var vm = new PropertiesPageViewModel
        {
            AddProperty = new AddPropertyViewModel
            {
                Name = "Alpha House",
                AddressLine = "Main 1",
                City = "Tallinn",
                PostalCode = "10111",
                PropertyTypeId = Guid.NewGuid(),
                IsActive = true
            }
        };

        var result = await controller.Add("north-estate", "acme", vm, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CustomerPropertiesController.Index), redirect.ActionName);
        Assert.Equal("north-estate", redirect.RouteValues?["companySlug"]);
        Assert.Equal("acme", redirect.RouteValues?["customerSlug"]);
        Assert.False(string.IsNullOrWhiteSpace(controller.TempData["CustomerPropertiesSuccess"]?.ToString()));
    }

    private static Mock<ICustomerWorkspaceService> BuildAuthorizedService(CustomerWorkspaceDashboardContext context)
    {
        var serviceMock = new Mock<ICustomerWorkspaceService>();
        serviceMock
            .Setup(x => x.ResolveDashboardAccessAsync(It.IsAny<Guid>(), "north-estate", "acme", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerDashboardAccessResult
            {
                IsAuthorized = true,
                Context = context
            });
        return serviceMock;
    }

    private static CustomerWorkspaceDashboardContext BuildDashboardContext()
    {
        return new CustomerWorkspaceDashboardContext
        {
            AppUserId = Guid.NewGuid(),
            ManagementCompanyId = Guid.NewGuid(),
            CompanySlug = "north-estate",
            CompanyName = "North Estate",
            CustomerId = Guid.NewGuid(),
            CustomerSlug = "acme",
            CustomerName = "Acme"
        };
    }

    private static CustomerPropertiesController CreateController(ICustomerWorkspaceService service, ClaimsPrincipal user)
    {
        var accessService = new Mock<ICustomerAccessService>();
        accessService
            .Setup(x => x.ResolveDashboardAccessAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, string companySlug, string customerSlug, CancellationToken _) =>
                service.ResolveDashboardAccessAsync(Guid.Empty, companySlug, customerSlug, CancellationToken.None).GetAwaiter().GetResult());

        var propertyService = new Mock<IPropertyWorkspaceService>();
        propertyService
            .Setup(x => x.ListPropertiesAsync(It.IsAny<CustomerWorkspaceDashboardContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerWorkspaceDashboardContext context, CancellationToken _) =>
                service.ListPropertiesAsync(context, CancellationToken.None).GetAwaiter().GetResult());
        propertyService
            .Setup(x => x.CreatePropertyAsync(It.IsAny<CustomerWorkspaceDashboardContext>(), It.IsAny<PropertyCreateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerWorkspaceDashboardContext context, PropertyCreateRequest request, CancellationToken _) =>
                service.CreatePropertyAsync(context, request, CancellationToken.None).GetAwaiter().GetResult());

        var controller = new CustomerPropertiesController(
            accessService.Object,
            propertyService.Object,
            Mock.Of<IAppChromeBuilder>(x => x.BuildAsync(It.IsAny<AppChromeRequest>(), It.IsAny<CancellationToken>()) == Task.FromResult(new AppChromeViewModel())),
            CreateDbContext(),
            Mock.Of<ILogger<CustomerPropertiesController>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            }
        };

        controller.TempData = new TempDataDictionary(
            controller.HttpContext,
            Mock.Of<ITempDataProvider>());

        return controller;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"customer-properties-controller-tests-{Guid.NewGuid()}")
            .Options;

        var db = new AppDbContext(options);
        db.PropertyTypes.Add(new PropertyType
        {
            Id = Guid.NewGuid(),
            Code = "APARTMENT_BUILDING",
            Label = new LangStr("Apartment building")
        });
        db.SaveChanges();
        return db;
    }

    private static ClaimsPrincipal BuildPrincipal(bool withNameIdentifier = true)
    {
        var claims = new List<Claim>();
        if (withNameIdentifier)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        }

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        return new ClaimsPrincipal(identity);
    }
}

