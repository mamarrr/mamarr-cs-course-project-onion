using System.Security.Claims;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.PropertyWorkspace.Properties;
using App.DAL.EF;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Customer.CustomerProperties;

namespace WebApp.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}/properties")]
public class CustomerPropertiesController : Controller
{
    private readonly ICustomerAccessService _customerAccessService;
    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CustomerPropertiesController> _logger;

    public CustomerPropertiesController(
        ICustomerAccessService customerAccessService,
        IPropertyWorkspaceService propertyWorkspaceService,
        IAppChromeBuilder appChromeBuilder,
        AppDbContext dbContext,
        ILogger<CustomerPropertiesController> logger)
    {
        _customerAccessService = customerAccessService;
        _propertyWorkspaceService = propertyWorkspaceService;
        _appChromeBuilder = appChromeBuilder;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string companySlug, string customerSlug, CancellationToken cancellationToken)
    {
        var access = await ResolveCustomerContextAsync(companySlug, customerSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var vm = await BuildPageViewModelAsync(access.context!, cancellationToken);
        return View(vm);
    }

    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        string companySlug,
        string customerSlug,
        PropertiesPageViewModel vm,
        CancellationToken cancellationToken)
    {
        var access = await ResolveCustomerContextAsync(companySlug, customerSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildPageViewModelAsync(access.context!, cancellationToken, vm.AddProperty);
            return View(nameof(Index), invalidVm);
        }

        var createResult = await _propertyWorkspaceService.CreatePropertyAsync(
            access.context!,
            new PropertyCreateRequest
            {
                Name = vm.AddProperty.Name,
                AddressLine = vm.AddProperty.AddressLine,
                City = vm.AddProperty.City,
                PostalCode = vm.AddProperty.PostalCode,
                PropertyTypeId = vm.AddProperty.PropertyTypeId!.Value,
                Notes = vm.AddProperty.Notes,
                IsActive = vm.AddProperty.IsActive
            },
            cancellationToken);

        if (!createResult.Success)
        {
            if (createResult.InvalidPropertyType)
            {
                ModelState.AddModelError(
                    "AddProperty.PropertyTypeId",
                    createResult.ErrorMessage ?? T("InvalidPropertyType", "Selected property type is invalid."));
            }
            else
            {
                ModelState.AddModelError(
                    string.Empty,
                    createResult.ErrorMessage ?? T("UnableToAddProperty", "Unable to add property."));
            }

            var invalidVm = await BuildPageViewModelAsync(access.context!, cancellationToken, vm.AddProperty);
            return View(nameof(Index), invalidVm);
        }

        _logger.LogInformation(
            "Customer property add succeeded. CompanySlug={CompanySlug}, CustomerSlug={CustomerSlug}, CreatedPropertyId={CreatedPropertyId}, CreatedPropertySlug={CreatedPropertySlug}",
            companySlug,
            customerSlug,
            createResult.CreatedPropertyId,
            createResult.CreatedPropertySlug);

        TempData["CustomerPropertiesSuccess"] = T("PropertyAddedSuccessfully", "Property added successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, customerSlug });
    }

    private async Task<(IActionResult? response, CustomerWorkspaceDashboardContext? context)> ResolveCustomerContextAsync(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (Challenge(), null);
        }

        var access = await _customerAccessService.ResolveDashboardAccessAsync(
            appUserId.Value,
            companySlug,
            customerSlug,
            cancellationToken);

        if (access.CompanyNotFound || access.CustomerNotFound)
        {
            return (NotFound(), null);
        }

        if (access.IsForbidden || access.Context == null)
        {
            return (Forbid(), null);
        }

        return (null, access.Context);
    }

    private async Task<PropertiesPageViewModel> BuildPageViewModelAsync(
        CustomerWorkspaceDashboardContext context,
        CancellationToken cancellationToken,
        AddPropertyViewModel? addPropertyOverride = null)
    {
        var listResult = await _propertyWorkspaceService.ListPropertiesAsync(context, cancellationToken);

        return new PropertiesPageViewModel
        {
            AppChrome = await BuildAppChromeAsync(context, UiText.Properties, cancellationToken),
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            Properties = listResult.Properties.Select(x => new CustomerPropertyListItemViewModel
            {
                PropertyId = x.PropertyId,
                PropertySlug = x.PropertySlug,
                PropertyName = x.PropertyName,
                AddressLine = x.AddressLine,
                City = x.City,
                PostalCode = x.PostalCode,
                PropertyTypeLabel = x.PropertyTypeLabel,
                IsActive = x.IsActive
            }).ToList(),
            AddProperty = addPropertyOverride ?? new AddPropertyViewModel(),
            PropertyTypeOptions = await _dbContext.PropertyTypes
                .AsNoTracking()
                .OrderBy(x => x.Code)
                .Select(x => new PropertyTypeOptionViewModel
                {
                    Id = x.Id,
                    Label = x.Label.ToString()
                })
                .ToListAsync(cancellationToken)
        };
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        CustomerWorkspaceDashboardContext context,
        string title,
        CancellationToken cancellationToken)
    {
        return _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = title,
                ActiveSection = Sections.Properties,
                ManagementCompanySlug = context.CompanySlug,
                ManagementCompanyName = context.CompanyName,
                CustomerSlug = context.CustomerSlug,
                CustomerName = context.CustomerName,
                CurrentLevel = WorkspaceLevel.Customer
            },
            cancellationToken);
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

