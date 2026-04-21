using System.Security.Claims;
using App.BLL.CustomerWorkspace.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApp.Services.SharedLayout;
using WebApp.ViewModels.Customer.CustomerDashboard;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}")]
public class CustomerDashboardController : Controller
{
    private readonly ICustomerAccessService _customerAccessService;
    private readonly IWorkspaceLayoutContextProvider _workspaceLayoutContextProvider;
    private readonly ILogger<CustomerDashboardController> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public CustomerDashboardController(
        ICustomerAccessService customerAccessService,
        IWorkspaceLayoutContextProvider workspaceLayoutContextProvider,
        ILogger<CustomerDashboardController> logger,
        IWebHostEnvironment webHostEnvironment)
    {
        _customerAccessService = customerAccessService;
        _workspaceLayoutContextProvider = workspaceLayoutContextProvider;
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

        var access = await _customerAccessService.ResolveDashboardAccessAsync(
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

        var customerLayout = new CustomerLayoutViewModel
        {
            CompanySlug = access.Context.CompanySlug,
            CompanyName = access.Context.CompanyName,
            CustomerSlug = access.Context.CustomerSlug,
            CustomerName = access.Context.CustomerName,
            CurrentSection = currentSection
        };

        var pageShell = await BuildPageShellAsync(
            title,
            sectionTitle,
            customerLayout.CompanySlug,
            cancellationToken,
            customerLayout);

        var vm = new CustomerDashboardPageViewModel
        {
            PageShell = pageShell,
            CompanySlug = access.Context.CompanySlug,
            CompanyName = access.Context.CompanyName,
            CustomerSlug = access.Context.CustomerSlug,
            CustomerName = access.Context.CustomerName
        };

        _logger.LogInformation(
            "Rendering customer dashboard section view. Controller={ControllerName}, Area={Area}, Section={Section}, ViewName={ViewName}",
            ControllerContext.ActionDescriptor.ControllerName,
            ControllerContext.RouteData.Values["area"],
            currentSection,
            "Index");

        return View("Index", vm);
    }

    private async Task<CustomerPageShellViewModel> BuildPageShellAsync(
        string title,
        string currentSectionLabel,
        string companySlug,
        CancellationToken cancellationToken,
        CustomerLayoutViewModel customerLayout)
    {
        var layoutContext = await _workspaceLayoutContextProvider.BuildAsync(
            User,
            BuildWorkspaceRequest(companySlug),
            cancellationToken);

        return new CustomerPageShellViewModel
        {
            Title = title,
            CurrentSectionLabel = currentSectionLabel,
            LayoutContext = layoutContext,
            Customer = customerLayout
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
