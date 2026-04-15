using System.Security.Claims;
using App.BLL.ManagementCustomers;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels.ManagementCustomers;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
[Authorize]
[Route("m/{companySlug}/customers")]
public class CustomersController : Controller
{
    private readonly IManagementCustomersService _managementCustomersService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        IManagementCustomersService managementCustomersService,
        ILogger<CustomersController> logger)
    {
        _managementCustomersService = managementCustomersService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string companySlug, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(companySlug, cancellationToken);
        if (authResult.response is not null)
        {
            return authResult.response;
        }

        var pageVm = await BuildPageViewModelAsync(authResult.context!, cancellationToken);
        return View(pageVm);
    }

    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        string companySlug,
        ManagementCustomersPageViewModel vm,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Management customers add started for companySlug={CompanySlug}, hasName={HasName}, hasRegistryCode={HasRegistryCode}",
            companySlug,
            !string.IsNullOrWhiteSpace(vm.AddCustomer.Name),
            !string.IsNullOrWhiteSpace(vm.AddCustomer.RegistryCode));

        var authResult = await AuthorizeAsync(companySlug, cancellationToken);
        if (authResult.response is not null)
        {
            _logger.LogWarning(
                "Management customers add authorization failed for companySlug={CompanySlug}, responseType={ResponseType}",
                companySlug,
                authResult.response.GetType().Name);
            return authResult.response;
        }

        if (!ModelState.IsValid)
        {
            var modelErrorCount = ModelState.Values.Sum(v => v.Errors.Count);
            var modelErrors = ModelState
                .Where(kvp => kvp.Value is { Errors.Count: > 0 })
                .SelectMany(kvp => kvp.Value!.Errors.Select(e => $"{kvp.Key}: {e.ErrorMessage}"))
                .ToArray();
            _logger.LogWarning(
                "Management customers add model validation failed for companySlug={CompanySlug}, errorCount={ErrorCount}, errors={Errors}",
                companySlug,
                modelErrorCount,
                modelErrors);
            
            var invalidVm = await BuildPageViewModelAsync(authResult.context!, cancellationToken, vm.AddCustomer);
            return View(nameof(Index), invalidVm);
        }

        var createResult = await _managementCustomersService.CreateAsync(
            authResult.context!,
            new ManagementCustomerCreateRequest
            {
                Name = vm.AddCustomer.Name,
                RegistryCode = vm.AddCustomer.RegistryCode,
                BillingEmail = vm.AddCustomer.BillingEmail,
                BillingAddress = vm.AddCustomer.BillingAddress,
                Phone = vm.AddCustomer.Phone
            },
            cancellationToken);

        if (!createResult.Success)
        {
            _logger.LogWarning(
                "Management customers add business validation failed for companySlug={CompanySlug}, duplicateRegistryCode={DuplicateRegistryCode}, invalidBillingEmail={InvalidBillingEmail}, errorMessage={ErrorMessage}",
                companySlug,
                createResult.DuplicateRegistryCode,
                createResult.InvalidBillingEmail,
                createResult.ErrorMessage);
            if (createResult.DuplicateRegistryCode)
            {
                ModelState.AddModelError("AddCustomer.RegistryCode", createResult.ErrorMessage ?? T("CustomerRegistryCodeAlreadyExists", "Customer with this registry code already exists in this company."));
            }
            else if (createResult.InvalidBillingEmail)
            {
                ModelState.AddModelError("AddCustomer.BillingEmail", createResult.ErrorMessage ?? UiText.InvalidEmailAddress);
            }
            else
            {
                ModelState.AddModelError(string.Empty, createResult.ErrorMessage ?? T("UnableToAddCustomer", "Unable to add customer."));
            }

            var invalidVm = await BuildPageViewModelAsync(authResult.context!, cancellationToken, vm.AddCustomer);
            return View(nameof(Index), invalidVm);
        }

        _logger.LogInformation(
            "Management customers add succeeded for companySlug={CompanySlug}, createdCustomerId={CreatedCustomerId}",
            companySlug,
            createResult.CreatedCustomerId);

        TempData["ManagementCustomersSuccess"] = T("CustomerAddedSuccessfully", "Customer added successfully.");
        return RedirectToAction(nameof(Index), new { companySlug });
    }

    private async Task<(IActionResult? response, ManagementCustomersAuthorizedContext? context)> AuthorizeAsync(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (Challenge(), null);
        }

        var auth = await _managementCustomersService.AuthorizeAsync(appUserId.Value, companySlug, cancellationToken);
        if (auth.CompanyNotFound)
        {
            return (NotFound(), null);
        }

        if (auth.IsForbidden)
        {
            return (Forbid(), null);
        }

        return (null, auth.Context);
    }

    private async Task<ManagementCustomersPageViewModel> BuildPageViewModelAsync(
        ManagementCustomersAuthorizedContext context,
        CancellationToken cancellationToken,
        AddManagementCustomerViewModel? addCustomerOverride = null)
    {
        var listResult = await _managementCustomersService.ListAsync(context, cancellationToken);

        ViewData["Title"] = UiText.Customers;

        return new ManagementCustomersPageViewModel
        {
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            Customers = listResult.Customers.Select(x => new ManagementCustomerListItemViewModel
            {
                CustomerId = x.CustomerId,
                Name = x.Name,
                RegistryCode = x.RegistryCode,
                BillingEmail = x.BillingEmail,
                BillingAddress = x.BillingAddress,
                Phone = x.Phone
            }).ToList(),
            AddCustomer = addCustomerOverride ?? new AddManagementCustomerViewModel()
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
