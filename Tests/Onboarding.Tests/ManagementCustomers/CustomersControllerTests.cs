using System.Security.Claims;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Customers;
using App.BLL.CustomerWorkspace.Workspace;
using App.DAL.EF;
using App.Domain;
using Base.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Areas.Management.Controllers;
using WebApp.Services.ManagementLayout;
using WebApp.ViewModels.Management.Layout;
using WebApp.ViewModels.ManagementCustomers;
using WebApp.ViewModels.Shared.Layout;
using Xunit;

namespace Onboarding.Tests.ManagementCustomers;

public class CustomersControllerTests
{
    [Fact]
    public async Task Index_ReturnsChallenge_WhenUserIdClaimMissing()
    {
        var serviceMock = new Mock<IManagementCustomersService>();
        var controller = CreateController(serviceMock.Object, BuildPrincipal(withNameIdentifier: false));

        var result = await controller.Index("north-estate", CancellationToken.None);

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsNotFound_WhenCompanySlugInvalid()
    {
        var serviceMock = new Mock<IManagementCustomersService>();
        serviceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<Guid>(), "missing-company", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagementCustomersAuthorizationResult { CompanyNotFound = true });

        var controller = CreateController(serviceMock.Object, BuildPrincipal());

        var result = await controller.Index("missing-company", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsForbid_WhenUnauthorized()
    {
        var serviceMock = new Mock<IManagementCustomersService>();
        serviceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<Guid>(), "north-estate", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagementCustomersAuthorizationResult { IsForbidden = true });

        var controller = CreateController(serviceMock.Object, BuildPrincipal());

        var result = await controller.Index("north-estate", CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Add_SuccessfulCreate_RedirectsToIndex_AndSetsTempDataSuccess()
    {
        var context = new ManagementCustomersAuthorizedContext
        {
            AppUserId = Guid.NewGuid(),
            ManagementCompanyId = Guid.NewGuid(),
            CompanySlug = "north-estate",
            CompanyName = "North Estate"
        };

        var serviceMock = new Mock<IManagementCustomersService>();
        serviceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<Guid>(), "north-estate", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagementCustomersAuthorizationResult
            {
                IsAuthorized = true,
                Context = context
            });

        serviceMock
            .Setup(x => x.CreateAsync(
                context,
                It.IsAny<ManagementCustomerCreateRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagementCustomerCreateResult { Success = true });

        var controller = CreateController(serviceMock.Object, BuildPrincipal());
        var vm = new ManagementCustomersPageViewModel
        {
            AddCustomer = new AddManagementCustomerViewModel
            {
                Name = "Acme Customer",
                RegistryCode = "ACME-001",
                BillingEmail = "billing@acme.test"
            }
        };

        var result = await controller.Add("north-estate", vm, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CustomersController.Index), redirect.ActionName);
        Assert.Equal("north-estate", redirect.RouteValues?["companySlug"]);
        Assert.False(string.IsNullOrWhiteSpace(controller.TempData["ManagementCustomersSuccess"]?.ToString()));
    }

    [Fact]
    public async Task Add_InvalidModel_ReturnsIndexView_WithValidationErrors()
    {
        var context = new ManagementCustomersAuthorizedContext
        {
            AppUserId = Guid.NewGuid(),
            ManagementCompanyId = Guid.NewGuid(),
            CompanySlug = "north-estate",
            CompanyName = "North Estate"
        };

        var serviceMock = new Mock<IManagementCustomersService>();
        serviceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<Guid>(), "north-estate", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagementCustomersAuthorizationResult
            {
                IsAuthorized = true,
                Context = context
            });

        serviceMock
            .Setup(x => x.ListAsync(context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagementCustomerListResult
            {
                Customers =
                [
                    new ManagementCustomerListItem
                    {
                        CustomerId = Guid.NewGuid(),
                        Name = "Existing",
                        RegistryCode = "EX-1"
                    }
                ]
            });

        var controller = CreateController(serviceMock.Object, BuildPrincipal());
        controller.ModelState.AddModelError("AddCustomer.Name", "Name is required");

        var vm = new ManagementCustomersPageViewModel
        {
            AddCustomer = new AddManagementCustomerViewModel
            {
                Name = string.Empty,
                RegistryCode = ""
            }
        };

        var result = await controller.Add("north-estate", vm, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey("AddCustomer.Name"));
        Assert.IsType<ManagementCustomersPageViewModel>(view.Model);
    }

    private static CustomersController CreateController(IManagementCustomersService service, ClaimsPrincipal user)
    {
        var dbContext = CreateDbContext();
        var accessService = new Mock<IManagementCustomerAccessService>();
        accessService
            .Setup(x => x.AuthorizeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, string companySlug, CancellationToken _) =>
                service.AuthorizeAsync(Guid.Empty, companySlug, CancellationToken.None).GetAwaiter().GetResult());

        var customerService = new Mock<IManagementCustomerService>();
        customerService
            .Setup(x => x.ListAsync(It.IsAny<ManagementCustomersAuthorizedContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManagementCustomersAuthorizedContext context, CancellationToken _) =>
                service.ListAsync(context, CancellationToken.None).GetAwaiter().GetResult());
        customerService
            .Setup(x => x.CreateAsync(It.IsAny<ManagementCustomersAuthorizedContext>(), It.IsAny<ManagementCustomerCreateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManagementCustomersAuthorizedContext context, ManagementCustomerCreateRequest request, CancellationToken _) =>
                service.CreateAsync(context, request, CancellationToken.None).GetAwaiter().GetResult());

        var controller = new CustomersController(
            accessService.Object,
            customerService.Object,
            dbContext,
            Mock.Of<ILogger<CustomersController>>(),
            Mock.Of<IManagementLayoutViewModelProvider>(x => x.BuildAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ManagementLayoutRequestViewModel>(), It.IsAny<CancellationToken>()) == Task.FromResult(new ManagementLayoutViewModel())))
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

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"customers-controller-tests-{Guid.NewGuid()}")
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
}

