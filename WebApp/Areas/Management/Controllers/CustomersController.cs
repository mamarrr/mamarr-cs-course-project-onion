using System.Security.Claims;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Customers;
using App.BLL.CustomerWorkspace.Workspace;
using App.DAL.EF;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Services.ManagementLayout;
using WebApp.ViewModels.ManagementCustomers;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
[Authorize]
[Route("m/{companySlug}/customers")]
public class CustomersController : ManagementPageShellController
{
    private readonly ICustomerAccessService _customerAccessService;
    private readonly ICompanyCustomerService _companyCustomerService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        ICustomerAccessService customerAccessService,
        ICompanyCustomerService companyCustomerService,
        AppDbContext dbContext,
        ILogger<CustomersController> logger,
        IManagementLayoutViewModelProvider managementLayoutViewModelProvider)
        : base(managementLayoutViewModelProvider)
    {
        _customerAccessService = customerAccessService;
        _companyCustomerService = companyCustomerService;
        _dbContext = dbContext;
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

        var createResult = await _companyCustomerService.CreateAsync(
            authResult.context!,
            new CustomerCreateRequest
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

    private async Task<(IActionResult? response, CustomerWorkspaceContext? context)> AuthorizeAsync(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (Challenge(), null);
        }

        var auth = await _customerAccessService.AuthorizeAsync(appUserId.Value, companySlug, cancellationToken);
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
        CustomerWorkspaceContext context,
        CancellationToken cancellationToken,
        AddManagementCustomerViewModel? addCustomerOverride = null)
    {
        var listResult = await _companyCustomerService.ListAsync(context, cancellationToken);
        var customerIds = listResult.Customers.Select(c => c.CustomerId).ToArray();

        var propertiesByCustomerId = await _dbContext.Properties
            .AsNoTracking()
            .Where(p => customerIds.Contains(p.CustomerId))
            .OrderBy(p => p.Label)
            .Select(p => new
            {
                p.CustomerId,
                p.Slug,
                Name = p.Label.ToString()
            })
            .ToListAsync(cancellationToken);

        var propertyLookup = propertiesByCustomerId
            .GroupBy(x => x.CustomerId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ManagementCustomerPropertyLinkViewModel>)g
                    .Select(x => new ManagementCustomerPropertyLinkViewModel
                    {
                        PropertySlug = x.Slug,
                        PropertyName = x.Name
                    })
                    .ToList());

        var title = UiText.Customers;

        return new ManagementCustomersPageViewModel
        {
            PageShell = await BuildManagementPageShellAsync(title, title, context.CompanySlug, cancellationToken),
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            Customers = listResult.Customers.Select(x => new ManagementCustomerListItemViewModel
            {
                CustomerId = x.CustomerId,
                CustomerSlug = x.CustomerSlug,
                Name = x.Name,
                RegistryCode = x.RegistryCode,
                BillingEmail = x.BillingEmail,
                BillingAddress = x.BillingAddress,
                Phone = x.Phone,
                Properties = propertyLookup.GetValueOrDefault(x.CustomerId, Array.Empty<ManagementCustomerPropertyLinkViewModel>())
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
