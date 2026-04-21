using System.Security.Claims;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.PropertyWorkspace.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApp.Areas.Customer.Controllers;
using WebApp.Areas.Property.Controllers;
using WebApp.Services.SharedLayout;
using WebApp.ViewModels.Management.CustomerProperties;
using WebApp.ViewModels.Shared.Layout;
using Xunit;

namespace Onboarding.Tests.ManagementCustomers;

public class CustomerPropertyDashboardControllerTests
{
    [Fact]
    public async Task Index_ReturnsChallenge_WhenUserIdClaimMissing()
    {
        var serviceMock = new Mock<ICustomerWorkspaceService>();
        var controller = CreateController(serviceMock.Object, BuildPrincipal(withNameIdentifier: false));

        var result = await controller.Index("north-estate", "acme", "alpha-house", CancellationToken.None);

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsNotFound_WhenCustomerContextMissing()
    {
        var serviceMock = new Mock<ICustomerWorkspaceService>();
        serviceMock
            .Setup(x => x.ResolveDashboardAccessAsync(It.IsAny<Guid>(), "north-estate", "acme", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerDashboardAccessResult { CustomerNotFound = true });

        var controller = CreateController(serviceMock.Object, BuildPrincipal());

        var result = await controller.Index("north-estate", "acme", "alpha-house", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsForbid_WhenCustomerContextForbidden()
    {
        var serviceMock = new Mock<ICustomerWorkspaceService>();
        serviceMock
            .Setup(x => x.ResolveDashboardAccessAsync(It.IsAny<Guid>(), "north-estate", "acme", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerDashboardAccessResult { IsForbidden = true });

        var controller = CreateController(serviceMock.Object, BuildPrincipal());

        var result = await controller.Index("north-estate", "acme", "alpha-house", CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsNotFound_WhenPropertyMissing()
    {
        var dashboardContext = BuildDashboardContext();
        var serviceMock = BuildServiceWithCustomerContext(dashboardContext);
        serviceMock
            .Setup(x => x.ResolvePropertyDashboardContextAsync(dashboardContext, "alpha-house", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyDashboardAccessResult { PropertyNotFound = true });

        var controller = CreateController(serviceMock.Object, BuildPrincipal());

        var result = await controller.Index("north-estate", "acme", "alpha-house", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsForbid_WhenPropertyContextUnauthorized()
    {
        var dashboardContext = BuildDashboardContext();
        var serviceMock = BuildServiceWithCustomerContext(dashboardContext);
        serviceMock
            .Setup(x => x.ResolvePropertyDashboardContextAsync(dashboardContext, "alpha-house", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyDashboardAccessResult { IsAuthorized = false });

        var controller = CreateController(serviceMock.Object, BuildPrincipal());

        var result = await controller.Index("north-estate", "acme", "alpha-house", CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsView_WhenAuthorized()
    {
        var dashboardContext = BuildDashboardContext();
        var propertyContext = new PropertyDashboardContext
        {
            AppUserId = dashboardContext.AppUserId,
            ManagementCompanyId = dashboardContext.ManagementCompanyId,
            CompanySlug = dashboardContext.CompanySlug,
            CompanyName = dashboardContext.CompanyName,
            CustomerId = dashboardContext.CustomerId,
            CustomerSlug = dashboardContext.CustomerSlug,
            CustomerName = dashboardContext.CustomerName,
            PropertyId = Guid.NewGuid(),
            PropertySlug = "alpha-house",
            PropertyName = "Alpha House"
        };

        var serviceMock = BuildServiceWithCustomerContext(dashboardContext);
        serviceMock
            .Setup(x => x.ResolvePropertyDashboardContextAsync(dashboardContext, "alpha-house", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyDashboardAccessResult
            {
                IsAuthorized = true,
                Context = propertyContext
            });

        var controller = CreateController(serviceMock.Object, BuildPrincipal());

        var result = await controller.Index("north-estate", "acme", "alpha-house", CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        var vm = Assert.IsType<PropertyDashboardPageViewModel>(view.Model);
        Assert.Equal("alpha-house", vm.PropertySlug);
        Assert.Equal("Dashboard", vm.CurrentSection);
    }

    private static Mock<ICustomerWorkspaceService> BuildServiceWithCustomerContext(CustomerWorkspaceDashboardContext context)
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

    private static PropertyDashboardController CreateController(ICustomerWorkspaceService service, ClaimsPrincipal user)
    {
        var accessService = new Mock<ICustomerAccessService>();
        accessService
            .Setup(x => x.ResolveDashboardAccessAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, string companySlug, string customerSlug, CancellationToken _) =>
                service.ResolveDashboardAccessAsync(Guid.Empty, companySlug, customerSlug, CancellationToken.None).GetAwaiter().GetResult());

        var propertyService = new Mock<IPropertyWorkspaceService>();
        propertyService
            .Setup(x => x.ResolvePropertyDashboardContextAsync(It.IsAny<CustomerWorkspaceDashboardContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerWorkspaceDashboardContext context, string propertySlug, CancellationToken _) =>
                service.ResolvePropertyDashboardContextAsync(context, propertySlug, CancellationToken.None).GetAwaiter().GetResult());

        return new PropertyDashboardController(
            accessService.Object,
            propertyService.Object,
            Mock.Of<IWorkspaceLayoutContextProvider>(x => x.BuildAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<WorkspaceLayoutRequestViewModel>(), It.IsAny<CancellationToken>()) == Task.FromResult(new WorkspaceLayoutContextViewModel())))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            }
        };
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

