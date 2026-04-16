using System.Security.Claims;
using App.BLL.Management;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services.SharedLayout;
using WebApp.ViewModels.Management.CustomerProperties;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.Areas.Property.Controllers;

[Area("Property")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}/p/{propertySlug}")]
public class PropertyDashboardController : Controller
{
    private readonly IManagementCustomerAccessService _managementCustomerAccessService;
    private readonly IManagementCustomerPropertyService _managementCustomerPropertyService;
    private readonly IWorkspaceLayoutContextProvider _workspaceLayoutContextProvider;

    public PropertyDashboardController(
        IManagementCustomerAccessService managementCustomerAccessService,
        IManagementCustomerPropertyService managementCustomerPropertyService,
        IWorkspaceLayoutContextProvider workspaceLayoutContextProvider)
    {
        _managementCustomerAccessService = managementCustomerAccessService;
        _managementCustomerPropertyService = managementCustomerPropertyService;
        _workspaceLayoutContextProvider = workspaceLayoutContextProvider;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, "Dashboard", cancellationToken);
    }

    [HttpGet("residents")]
    public async Task<IActionResult> Residents(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, "Residents", cancellationToken);
    }

    [HttpGet("tickets")]
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
        if (appUserId == null)
        {
            return Challenge();
        }

        var customerAccess = await _managementCustomerAccessService.ResolveDashboardAccessAsync(
            appUserId.Value,
            companySlug,
            customerSlug,
            cancellationToken);

        if (customerAccess.CompanyNotFound || customerAccess.CustomerNotFound)
        {
            return NotFound();
        }

        if (customerAccess.IsForbidden || customerAccess.Context == null)
        {
            return Forbid();
        }

        var propertyAccess = await _managementCustomerPropertyService.ResolvePropertyDashboardContextAsync(
            customerAccess.Context,
            propertySlug,
            cancellationToken);

        if (propertyAccess.PropertyNotFound)
        {
            return NotFound();
        }

        if (!propertyAccess.IsAuthorized || propertyAccess.Context == null)
        {
            return Forbid();
        }

        var currentSectionLabel = currentSection switch
        {
            "Profile" => UiText.Profile,
            "Residents" => UiText.Residents,
            "Tickets" => UiText.Tickets,
            _ => UiText.Dashboard
        };

        var propertyLayout = new PropertyLayoutViewModel
        {
            CompanySlug = propertyAccess.Context.CompanySlug,
            CompanyName = propertyAccess.Context.CompanyName,
            CustomerSlug = propertyAccess.Context.CustomerSlug,
            CustomerName = propertyAccess.Context.CustomerName,
            PropertySlug = propertyAccess.Context.PropertySlug,
            PropertyName = propertyAccess.Context.PropertyName,
            CurrentSection = currentSection
        };

        var pageShell = await BuildPageShellAsync(
            T("PropertyDashboard", "Property dashboard"),
            currentSectionLabel,
            propertyLayout.CompanySlug,
            cancellationToken,
            propertyLayout);

        var vm = new PropertyDashboardPageViewModel
        {
            PageShell = pageShell,
            CompanySlug = propertyAccess.Context.CompanySlug,
            CompanyName = propertyAccess.Context.CompanyName,
            CustomerSlug = propertyAccess.Context.CustomerSlug,
            CustomerName = propertyAccess.Context.CustomerName,
            PropertySlug = propertyAccess.Context.PropertySlug,
            PropertyName = propertyAccess.Context.PropertyName,
            CurrentSection = currentSection
        };

        return View("Index", vm);
    }

    private async Task<PropertyPageShellViewModel> BuildPageShellAsync(
        string title,
        string currentSectionLabel,
        string companySlug,
        CancellationToken cancellationToken,
        PropertyLayoutViewModel propertyLayout)
    {
        var layoutContext = await _workspaceLayoutContextProvider.BuildAsync(
            User,
            BuildWorkspaceRequest(companySlug),
            cancellationToken);

        return new PropertyPageShellViewModel
        {
            Title = title,
            CurrentSectionLabel = currentSectionLabel,
            LayoutContext = layoutContext,
            Property = propertyLayout
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
