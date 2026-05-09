using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Dashboards.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers.Dashboards;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Resident;

namespace WebApp.Areas.Portal.Controllers.Resident;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/r/{residentIdCode}")]
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

    [HttpGet("", Name = PortalRouteNames.ResidentDashboard)]
    public async Task<IActionResult> Index(string companySlug, string residentIdCode, CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, residentIdCode, "Dashboard", cancellationToken);
    }

    [HttpGet("tickets", Name = PortalRouteNames.ResidentTickets)]
    public async Task<IActionResult> Tickets(string companySlug, string residentIdCode, CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, residentIdCode, "Tickets", cancellationToken);
    }

    [HttpGet("representations", Name = PortalRouteNames.ResidentRepresentations)]
    public async Task<IActionResult> Representations(string companySlug, string residentIdCode, CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, residentIdCode, "Representations", cancellationToken);
    }

    private async Task<IActionResult> RenderSectionAsync(
        string companySlug,
        string residentIdCode,
        string currentSection,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var dashboard = await _bll.PortalDashboards.GetResidentDashboardAsync(
            new ResidentRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                ResidentIdCode = residentIdCode
            },
            cancellationToken);
        if (dashboard.IsFailed)
        {
            return ToFailureResult(dashboard.Errors);
        }

        var context = dashboard.Value.Context;
        var currentSectionLabel = currentSection switch
        {
            "Units" => T("Units", "Units"),
            "Tickets" => UiText.Tickets,
            "Representations" => T("Representations", "Representations"),
            "Contacts" => T("Contacts", "Contacts"),
            _ => UiText.Dashboard
        };

        var title = currentSection == "Dashboard"
            ? T("ResidentDashboard", "Resident dashboard")
            : currentSectionLabel;

        var residentDisplayName = string.IsNullOrWhiteSpace(context.FullName)
            ? context.ResidentIdCode
            : context.FullName;
        var residentSupportingText = string.IsNullOrWhiteSpace(context.FullName)
            ? null
            : context.ResidentIdCode;

        var vm = new DashboardPageViewModel
        {
            AppChrome = await BuildAppChromeAsync(context, title, currentSection, cancellationToken),
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentIdCode = context.ResidentIdCode,
            ResidentDisplayName = residentDisplayName,
            ResidentSupportingText = residentSupportingText,
            CurrentSection = currentSection,
            CurrentSectionLabel = currentSectionLabel,
            Dashboard = PortalDashboardViewModelMapper.Map(dashboard.Value)
        };

        return View("Index", vm);
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        ResidentDashboardContextModel context,
        string title,
        string activeSection,
        CancellationToken cancellationToken)
    {
        var residentDisplayName = string.IsNullOrWhiteSpace(context.FullName)
            ? context.ResidentIdCode
            : context.FullName;

        return _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = title,
                ActiveSection = activeSection,
                ManagementCompanySlug = context.CompanySlug,
                ManagementCompanyName = context.CompanyName,
                ResidentIdCode = context.ResidentIdCode,
                ResidentDisplayName = residentDisplayName,
                ResidentSupportingText = string.IsNullOrWhiteSpace(context.FullName) ? null : context.ResidentIdCode,
                CurrentLevel = WorkspaceLevel.Resident
            },
            cancellationToken);
    }

    private IActionResult ToFailureResult(IReadOnlyList<IError> errors)
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
