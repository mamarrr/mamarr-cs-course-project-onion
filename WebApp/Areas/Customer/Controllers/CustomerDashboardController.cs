using System.Security.Claims;
using App.BLL.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApp.ViewModels.Customer.CustomerDashboard;

namespace WebApp.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}")]
public class CustomerDashboardController : Controller
{
    private readonly IManagementCustomerAccessService _managementCustomerAccessService;
    private readonly ILogger<CustomerDashboardController> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public CustomerDashboardController(
        IManagementCustomerAccessService managementCustomerAccessService,
        ILogger<CustomerDashboardController> logger,
        IWebHostEnvironment webHostEnvironment)
    {
        _managementCustomerAccessService = managementCustomerAccessService;
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet("")]
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

    [HttpGet("profile")]
    public async Task<IActionResult> Profile(string companySlug, string customerSlug, CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, "Profile", cancellationToken);
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> Tickets(string companySlug, string customerSlug, CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, "Tickets", cancellationToken);
    }

    [HttpGet("residents")]
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
        LogViewCandidates(currentSection);

        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return Challenge();
        }

        var access = await _managementCustomerAccessService.ResolveDashboardAccessAsync(
            appUserId.Value,
            companySlug,
            customerSlug,
            cancellationToken);

        if (access.CompanyNotFound || access.CustomerNotFound)
        {
            return NotFound();
        }

        if (access.IsForbidden || access.Context == null)
        {
            return Forbid();
        }

        var vm = new CustomerDashboardPageViewModel
        {
            CompanySlug = access.Context.CompanySlug,
            CompanyName = access.Context.CompanyName,
            CustomerSlug = access.Context.CustomerSlug,
            CustomerName = access.Context.CustomerName
        };

        var sectionTitle = currentSection switch
        {
            "Profile" => T("Profile", "Profile"),
            "Tickets" => App.Resources.Views.UiText.Tickets,
            "Residents" => T("Residents", "Residents"),
            _ => App.Resources.Views.UiText.Dashboard
        };

        ViewData["Title"] = currentSection == "Dashboard"
            ? T("CustomerDashboard", "Customer dashboard")
            : sectionTitle;

        ViewData["CurrentSectionLabel"] = sectionTitle;
        ViewData["CustomerLayout"] = new CustomerLayoutViewModel
        {
            CompanySlug = access.Context.CompanySlug,
            CompanyName = access.Context.CompanyName,
            CustomerSlug = access.Context.CustomerSlug,
            CustomerName = access.Context.CustomerName,
            CurrentSection = currentSection
        };

        _logger.LogInformation(
            "Rendering customer dashboard section view. Controller={ControllerName}, Area={Area}, Section={Section}, ViewName={ViewName}",
            ControllerContext.ActionDescriptor.ControllerName,
            ControllerContext.RouteData.Values["area"],
            currentSection,
            "Index");

        return View("Index", vm);
    }

    private void LogViewCandidates(string currentSection)
    {
        var relativeCandidates = new[]
        {
            "Areas/Customer/Views/CustomerDashboard/Index.cshtml",
            "Areas/Customer/Views/Dashboard/Index.cshtml",
            "Areas/Customer/Views/Shared/Index.cshtml",
            "Views/Shared/Index.cshtml"
        };

        foreach (var relativePath in relativeCandidates)
        {
            var physicalPath = Path.Combine(
                _webHostEnvironment.ContentRootPath,
                relativePath.Replace('/', Path.DirectorySeparatorChar));

            _logger.LogInformation(
                "Customer dashboard view candidate. Section={Section}, RelativePath={RelativePath}, PhysicalPath={PhysicalPath}, Exists={Exists}",
                currentSection,
                relativePath,
                physicalPath,
                System.IO.File.Exists(physicalPath));
        }
    }

    private Guid? GetAppUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }

    private static string T(string key, string fallback)
    {
        return App.Resources.Views.UiText.ResourceManager.GetString(key) ?? fallback;
    }
}

