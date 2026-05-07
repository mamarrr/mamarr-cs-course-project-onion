using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Properties.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Property;

namespace WebApp.Areas.Portal.Controllers.Property;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/customers/{customerSlug}/properties/{propertySlug}")]
public class DashboardController : Controller
{
    private readonly IAppBLL _bll;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public DashboardController(
        IAppBLL bll,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver)
    {
        _bll = bll;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
    }

    [HttpGet("", Name = PortalRouteNames.PropertyDashboard)]
    public async Task<IActionResult> Index(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, "Dashboard", cancellationToken);
    }

    [HttpGet("residents", Name = PortalRouteNames.PropertyResidents)]
    public async Task<IActionResult> Residents(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, "Residents", cancellationToken);
    }

    [HttpGet("tickets", Name = PortalRouteNames.PropertyTickets)]
    public async Task<IActionResult> Tickets(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, "Tickets", cancellationToken);
    }

    private async Task<IActionResult> RenderSectionAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string currentSection,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var result = await _bll.Properties.GetWorkspaceAsync(
            new PropertyRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                CustomerSlug = customerSlug,
                PropertySlug = propertySlug
            },
            cancellationToken);
        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        var currentSectionLabel = currentSection switch
        {
            "Profile" => UiText.Profile,
            "Residents" => UiText.Residents,
            "Tickets" => UiText.Tickets,
            _ => UiText.Dashboard
        };

        var title = currentSection == Sections.Dashboard
            ? T("PropertyDashboard", "Property dashboard")
            : currentSectionLabel;

        var vm = new DashboardPageViewModel
        {
            AppChrome = await BuildAppChromeAsync(result.Value, title, currentSection, cancellationToken),
            CompanySlug = result.Value.CompanySlug,
            CompanyName = result.Value.CompanyName,
            CustomerSlug = result.Value.CustomerSlug,
            CustomerName = result.Value.CustomerName,
            PropertySlug = result.Value.PropertySlug,
            PropertyName = result.Value.PropertyName,
            CurrentSection = currentSection,
            CurrentSectionLabel = currentSectionLabel
        };

        return View("Index", vm);
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        PropertyWorkspaceModel context,
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
                PropertySlug = context.PropertySlug,
                PropertyName = context.PropertyName,
                CurrentLevel = WorkspaceLevel.Property
            },
            cancellationToken);
    }

    private IActionResult ToMvcErrorResult(IReadOnlyList<IError> errors)
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
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
