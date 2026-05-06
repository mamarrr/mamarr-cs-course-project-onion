using App.BLL.Contracts;
using App.BLL.Contracts.Common.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApp.Mappers.Api.Customers;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Customer.CustomerDashboard;

namespace WebApp.Areas.Portal.Controllers.Customer;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/customers/{customerSlug}")]
public class CustomerDashboardController : Controller
{
    private readonly IAppBLL _bll;
    private readonly CustomerWorkspaceApiMapper _mapper;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;
    private readonly ILogger<CustomerDashboardController> _logger;

    public CustomerDashboardController(
        IAppBLL bll,
        CustomerWorkspaceApiMapper mapper,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver,
        ILogger<CustomerDashboardController> logger)
    {
        _bll = bll;
        _mapper = mapper;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
        _logger = logger;
    }

    [HttpGet("", Name = PortalRouteNames.CustomerDashboard)]
    public async Task<IActionResult> Index(string companySlug, string customerSlug, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Customer dashboard index requested. Controller={ControllerName}, Area={Area}, RouteCompanySlug={CompanySlug}, RouteCustomerSlug={CustomerSlug}",
            ControllerContext.ActionDescriptor.ControllerName,
            ControllerContext.RouteData.Values["area"],
            companySlug,
            customerSlug);

        return await RenderSectionAsync(companySlug, customerSlug, "Dashboard", cancellationToken);
    }

    [HttpGet("tickets", Name = PortalRouteNames.CustomerTickets)]
    public async Task<IActionResult> Tickets(string companySlug, string customerSlug, CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, "Tickets", cancellationToken);
    }

    [HttpGet("residents", Name = PortalRouteNames.CustomerResidents)]
    public async Task<IActionResult> Residents(string companySlug, string customerSlug, CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, "Residents", cancellationToken);
    }

    private async Task<IActionResult> RenderSectionAsync(
        string companySlug,
        string customerSlug,
        string currentSection,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var result = await _bll.CustomerWorkspaces.GetWorkspaceAsync(
            _mapper.ToQuery(companySlug, customerSlug, appUserId.Value),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        var sectionTitle = currentSection switch
        {
            "Profile" => T("Profile", "Profile"),
            "Tickets" => App.Resources.Views.UiText.Tickets,
            "Residents" => T("Residents", "Residents"),
            _ => App.Resources.Views.UiText.Dashboard
        };

        var title = currentSection == "Dashboard"
            ? T("CustomerDashboard", "Customer dashboard")
            : sectionTitle;

        var vm = new DashboardPageViewModel
        {
            AppChrome = await BuildAppChromeAsync(result.Value, title, currentSection, cancellationToken),
            CompanySlug = result.Value.CompanySlug,
            CompanyName = result.Value.CompanyName,
            CustomerSlug = result.Value.CustomerSlug,
            CustomerName = result.Value.CustomerName,
            CurrentSection = currentSection,
            CurrentSectionLabel = sectionTitle
        };

        _logger.LogInformation(
            "Rendering customer dashboard section view. Controller={ControllerName}, Area={Area}, Section={Section}, ViewName={ViewName}",
            ControllerContext.ActionDescriptor.ControllerName,
            ControllerContext.RouteData.Values["area"],
            currentSection,
            "Index");

        return View("Index", vm);
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        App.BLL.Contracts.Customers.Models.CustomerWorkspaceModel context,
        string title,
        string activeSection,
        CancellationToken cancellationToken)
    {
        return _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = title,
                ActiveSection = activeSection,
                ManagementCompanySlug = context.CompanySlug,
                ManagementCompanyName = context.CompanyName,
                CustomerSlug = context.CustomerSlug,
                CustomerName = context.CustomerName,
                CurrentLevel = WorkspaceLevel.Customer
            },
            cancellationToken);
    }

    private IActionResult ToMvcErrorResult(IReadOnlyList<FluentResults.IError> errors)
    {
        var error = errors.FirstOrDefault();
        return error switch
        {
            UnauthorizedError => Challenge(),
            NotFoundError => NotFound(),
            ForbiddenError => Forbid(),
            _ => BadRequest()
        };
    }

    private Guid? GetAppUserId()
    {
        return _portalContextResolver.Resolve().AppUserId;
    }

    private static string T(string key, string fallback)
    {
        return App.Resources.Views.UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
