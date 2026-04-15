using System.Security.Claims;
using App.BLL.Management;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels.Management.CustomerProperties;

namespace WebApp.Areas.Property.Controllers;

[Area("Property")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}/p/{propertySlug}")]
public class PropertyDashboardController : Controller
{
    private readonly IManagementCustomerAccessService _managementCustomerAccessService;
    private readonly IManagementCustomerPropertyService _managementCustomerPropertyService;

    public PropertyDashboardController(
        IManagementCustomerAccessService managementCustomerAccessService,
        IManagementCustomerPropertyService managementCustomerPropertyService)
    {
        _managementCustomerAccessService = managementCustomerAccessService;
        _managementCustomerPropertyService = managementCustomerPropertyService;
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

    [HttpGet("profile")]
    public async Task<IActionResult> Profile(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, "Profile", cancellationToken);
    }

    [HttpGet("units")]
    public async Task<IActionResult> Units(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, "Units", cancellationToken);
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

        ViewData["Title"] = T("PropertyDashboard", "Property dashboard");
        ViewData["CurrentSectionLabel"] = currentSection switch
        {
            "Profile" => UiText.Profile,
            "Units" => T("Units", "Units"),
            "Residents" => UiText.Residents,
            "Tickets" => UiText.Tickets,
            _ => UiText.Dashboard
        };

        ViewData["PropertyLayout"] = new PropertyLayoutViewModel
        {
            CompanySlug = propertyAccess.Context.CompanySlug,
            CompanyName = propertyAccess.Context.CompanyName,
            CustomerSlug = propertyAccess.Context.CustomerSlug,
            CustomerName = propertyAccess.Context.CustomerName,
            PropertySlug = propertyAccess.Context.PropertySlug,
            PropertyName = propertyAccess.Context.PropertyName,
            CurrentSection = currentSection
        };

        var vm = new PropertyDashboardPageViewModel
        {
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

