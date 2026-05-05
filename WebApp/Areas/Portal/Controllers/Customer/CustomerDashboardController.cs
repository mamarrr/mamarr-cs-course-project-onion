using App.BLL.Contracts;
using App.BLL.Contracts.Common.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApp.Mappers.Api.Customers;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Customer.CustomerDashboard;

namespace WebApp.Areas.Portal.Controllers.Customer;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}")]
public class CustomerDashboardController : Controller
{
    private readonly IAppBLL _bll;
    private readonly CustomerWorkspaceApiMapper _mapper;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ILogger<CustomerDashboardController> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public CustomerDashboardController(
        IAppBLL bll,
        CustomerWorkspaceApiMapper mapper,
        IAppChromeBuilder appChromeBuilder,
        ILogger<CustomerDashboardController> logger,
        IWebHostEnvironment webHostEnvironment)
    {
        _bll = bll;
        _mapper = mapper;
        _appChromeBuilder = appChromeBuilder;
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

        var result = await _bll.CustomerWorkspaces.GetWorkspaceAsync(
            _mapper.ToQuery(companySlug, customerSlug, User),
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
            CustomerName = result.Value.CustomerName
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

    private void LogViewCandidates(string currentSection)
    {
        var relativeCandidates = new[]
        {
            "Areas/Portal/Views/Customer/CustomerDashboard/Index.cshtml",
            "Areas/Portal/Views/Customer/Dashboard/Index.cshtml",
            "Areas/Portal/Views/Customer/Shared/Index.cshtml",
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

    private static string T(string key, string fallback)
    {
        return App.Resources.Views.UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
