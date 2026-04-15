using System.Security.Claims;
using App.BLL.ManagementCustomers;
using App.DAL.EF;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels.Customer.CustomerDashboard;
using WebApp.ViewModels.Management.CustomerProperties;

namespace WebApp.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}/properties")]
public class CustomerPropertiesController : Controller
{
    private readonly IManagementCustomersService _managementCustomersService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CustomerPropertiesController> _logger;

    public CustomerPropertiesController(
        IManagementCustomersService managementCustomersService,
        AppDbContext dbContext,
        ILogger<CustomerPropertiesController> logger)
    {
        _managementCustomersService = managementCustomersService;
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
        CustomerPropertiesPageViewModel vm,
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

        var createResult = await _managementCustomersService.CreatePropertyAsync(
            access.context!,
            new ManagementCustomerPropertyCreateRequest
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

    private async Task<(IActionResult? response, ManagementCustomerDashboardContext? context)> ResolveCustomerContextAsync(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (Challenge(), null);
        }

        var access = await _managementCustomersService.ResolveDashboardAccessAsync(
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

    private async Task<CustomerPropertiesPageViewModel> BuildPageViewModelAsync(
        ManagementCustomerDashboardContext context,
        CancellationToken cancellationToken,
        AddPropertyViewModel? addPropertyOverride = null)
    {
        var listResult = await _managementCustomersService.ListPropertiesAsync(context, cancellationToken);

        ViewData["Title"] = UiText.Properties;
        ViewData["CustomerLayout"] = new CustomerLayoutViewModel
        {
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            CurrentSection = "Properties"
        };

        return new CustomerPropertiesPageViewModel
        {
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
