using System.Security.Claims;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.PropertyWorkspace.Properties;
using App.BLL.UnitWorkspace.Access;
using App.BLL.UnitWorkspace.Workspace;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services.SharedLayout;
using WebApp.ViewModels.Shared.Layout;
using WebApp.ViewModels.Unit;

namespace WebApp.Areas.Unit.Controllers;

[Area("Unit")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}/p/{propertySlug}/u/{unitSlug}")]
public class UnitDashboardController : Controller
{
    private readonly ICustomerAccessService _customerAccessService;
    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly IUnitAccessService _unitAccessService;
    private readonly IWorkspaceLayoutContextProvider _workspaceLayoutContextProvider;

    public UnitDashboardController(
        ICustomerAccessService customerAccessService,
        IPropertyWorkspaceService propertyWorkspaceService,
        IUnitAccessService unitAccessService,
        IWorkspaceLayoutContextProvider workspaceLayoutContextProvider)
    {
        _customerAccessService = customerAccessService;
        _propertyWorkspaceService = propertyWorkspaceService;
        _unitAccessService = unitAccessService;
        _workspaceLayoutContextProvider = workspaceLayoutContextProvider;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, unitSlug, "Dashboard", cancellationToken);
    }

    [HttpGet("details")]
    public async Task<IActionResult> Details(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, unitSlug, "Details", cancellationToken);
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> Tickets(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, unitSlug, "Tickets", cancellationToken);
    }

    private async Task<IActionResult> RenderSectionAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        string currentSection,
        CancellationToken cancellationToken)
    {
        var access = await ResolveUnitContextAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var context = access.context!;
        ViewData["Title"] = T("UnitDashboard", "Unit dashboard");
        ViewData["CurrentSectionLabel"] = currentSection switch
        {
            "Details" => T("Details", "Details"),
            "Tenants" => T("Tenants", "Tenants"),
            "Tickets" => UiText.Tickets,
            _ => UiText.Dashboard
        };

        var unitLayout = new UnitLayoutViewModel
        {
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitSlug = context.UnitSlug,
            UnitName = context.UnitNr,
            CurrentSection = currentSection
        };

        var pageShell = await BuildPageShellAsync(
            ViewData["Title"] as string ?? T("UnitDashboard", "Unit dashboard"),
            ViewData["CurrentSectionLabel"] as string ?? UiText.Dashboard,
            context.CompanySlug,
            cancellationToken,
            unitLayout);

        var vm = new UnitDashboardPageViewModel
        {
            PageShell = pageShell,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitSlug = context.UnitSlug,
            UnitName = context.UnitNr,
            CurrentSection = currentSection
        };

        return View("Index", vm);
    }

    private async Task<UnitPageShellViewModel> BuildPageShellAsync(
        string title,
        string currentSectionLabel,
        string companySlug,
        CancellationToken cancellationToken,
        UnitLayoutViewModel unitLayout)
    {
        var layoutContext = await _workspaceLayoutContextProvider.BuildAsync(
            User,
            BuildWorkspaceRequest(companySlug),
            cancellationToken);

        return new UnitPageShellViewModel
        {
            Title = title,
            CurrentSectionLabel = currentSectionLabel,
            LayoutContext = layoutContext,
            Unit = unitLayout
        };
    }

    private WorkspaceLayoutRequestViewModel BuildWorkspaceRequest(string companySlug)
    {
        return new WorkspaceLayoutRequestViewModel
        {
            CurrentController = ControllerContext.ActionDescriptor.ControllerName,
            CompanySlug = companySlug,
            CurrentPathAndQuery = $"{Request.Path}{Request.QueryString}",
            CurrentUiCultureName = Thread.CurrentThread.CurrentUICulture.Name
        };
    }

    private async Task<(IActionResult? response, UnitDashboardContext? context)> ResolveUnitContextAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (Challenge(), null);
        }

        var customerAccess = await _customerAccessService.ResolveDashboardAccessAsync(
            appUserId.Value,
            companySlug,
            customerSlug,
            cancellationToken);

        if (customerAccess.CompanyNotFound || customerAccess.CustomerNotFound)
        {
            return (NotFound(), null);
        }

        if (customerAccess.IsForbidden || customerAccess.Context == null)
        {
            return (Forbid(), null);
        }

        var propertyAccess = await _propertyWorkspaceService.ResolvePropertyDashboardContextAsync(
            customerAccess.Context,
            propertySlug,
            cancellationToken);

        if (propertyAccess.PropertyNotFound)
        {
            return (NotFound(), null);
        }

        if (!propertyAccess.IsAuthorized || propertyAccess.Context == null)
        {
            return (Forbid(), null);
        }

        var unitAccess = await _unitAccessService.ResolveUnitDashboardContextAsync(
            propertyAccess.Context,
            unitSlug,
            cancellationToken);

        if (unitAccess.UnitNotFound)
        {
            return (NotFound(), null);
        }

        if (!unitAccess.IsAuthorized || unitAccess.Context == null)
        {
            return (Forbid(), null);
        }

        return (null, unitAccess.Context);
    }

    private Guid? GetAppUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
