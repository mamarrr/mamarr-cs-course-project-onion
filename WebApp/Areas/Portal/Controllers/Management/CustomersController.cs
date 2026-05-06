using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Customers.Errors;
using App.BLL.DTO.Customers.Queries;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers.Mvc.Customers;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.Customers;

namespace WebApp.Areas.Portal.Controllers.Management;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/customers")]
public class CustomersController : Controller
{
    private readonly IAppBLL _bll;
    private readonly CompanyCustomerMvcMapper _mapper;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        IAppBLL bll,
        CompanyCustomerMvcMapper mapper,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver,
        ILogger<CustomersController> logger)
    {
        _bll = bll;
        _mapper = mapper;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
        _logger = logger;
    }

    [HttpGet("", Name = PortalRouteNames.ManagementCustomers)]
    public async Task<IActionResult> Index(string companySlug, CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var query = _mapper.ToQuery(companySlug, appUserId.Value);
        var pageVm = await BuildPageViewModelAsync(query, cancellationToken);
        if (pageVm.response is not null)
        {
            return pageVm.response;
        }

        return View(pageVm.model);
    }

    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        string companySlug,
        CustomersPageViewModel vm,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        _logger.LogInformation(
            "Management customers add started for companySlug={CompanySlug}, hasName={HasName}, hasRegistryCode={HasRegistryCode}",
            companySlug,
            !string.IsNullOrWhiteSpace(vm.AddCustomer.Name),
            !string.IsNullOrWhiteSpace(vm.AddCustomer.RegistryCode));

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

            var invalidVm = await BuildPageViewModelAsync(
                _mapper.ToQuery(companySlug, appUserId.Value),
                cancellationToken,
                vm.AddCustomer);
            if (invalidVm.response is not null)
            {
                return invalidVm.response;
            }

            return View(nameof(Index), invalidVm.model);
        }

        var createResult = await _bll.CompanyCustomers.CreateCustomerAsync(
            _mapper.ToCommand(companySlug, vm.AddCustomer, appUserId.Value),
            cancellationToken);

        if (createResult.IsFailed)
        {
            var duplicateRegistryCode = createResult.Errors.Any(error => error is DuplicateRegistryCodeError);
            var invalidBillingEmail = createResult.Errors
                .OfType<ValidationAppError>()
                .Any(error => error.Failures.Any(f => f.PropertyName == nameof(vm.AddCustomer.BillingEmail)));

            _logger.LogWarning(
                "Management customers add business validation failed for companySlug={CompanySlug}, duplicateRegistryCode={DuplicateRegistryCode}, invalidBillingEmail={InvalidBillingEmail}, errorMessage={ErrorMessage}",
                companySlug,
                duplicateRegistryCode,
                invalidBillingEmail,
                createResult.Errors.FirstOrDefault()?.Message);

            if (duplicateRegistryCode)
            {
                ModelState.AddModelError("AddCustomer.RegistryCode", createResult.Errors.FirstOrDefault()?.Message ?? T("CustomerRegistryCodeAlreadyExists", "Customer with this registry code already exists in this company."));
            }
            else if (invalidBillingEmail)
            {
                ModelState.AddModelError("AddCustomer.BillingEmail", createResult.Errors.FirstOrDefault()?.Message ?? UiText.InvalidEmailAddress);
            }
            else if (createResult.Errors.Any(error => error is NotFoundError or ForbiddenError or UnauthorizedError))
            {
                return ToMvcErrorResult(createResult.Errors);
            }
            else
            {
                ModelState.AddModelError(string.Empty, createResult.Errors.FirstOrDefault()?.Message ?? T("UnableToAddCustomer", "Unable to add customer."));
            }

            var invalidVm = await BuildPageViewModelAsync(
                _mapper.ToQuery(companySlug, appUserId.Value),
                cancellationToken,
                vm.AddCustomer);
            if (invalidVm.response is not null)
            {
                return invalidVm.response;
            }

            return View(nameof(Index), invalidVm.model);
        }

        _logger.LogInformation(
            "Management customers add succeeded for companySlug={CompanySlug}, createdCustomerId={CreatedCustomerId}",
            companySlug,
            createResult.Value.CustomerId);

        TempData["ManagementCustomersSuccess"] = T("CustomerAddedSuccessfully", "Customer added successfully.");
        return RedirectToAction(nameof(Index), new { companySlug });
    }

    private async Task<(IActionResult? response, CustomersPageViewModel? model)> BuildPageViewModelAsync(
        GetCompanyCustomersQuery query,
        CancellationToken cancellationToken,
        AddManagementCustomerViewModel? addCustomerOverride = null)
    {
        var company = await _bll.CustomerAccess.ResolveCompanyWorkspaceAsync(query, cancellationToken);
        if (company.IsFailed)
        {
            return (ToMvcErrorResult(company.Errors), null);
        }

        var listResult = await _bll.CompanyCustomers.GetCompanyCustomersAsync(query, cancellationToken);
        if (listResult.IsFailed)
        {
            return (ToMvcErrorResult(listResult.Errors), null);
        }

        var title = UiText.Customers;

        return (null, new CustomersPageViewModel
        {
            AppChrome = await _appChromeBuilder.BuildAsync(
                new AppChromeRequest
                {
                    User = User,
                    HttpContext = HttpContext,
                    PageTitle = title,
                    ActiveSection = Sections.Customers,
                    ManagementCompanySlug = company.Value.CompanySlug,
                    ManagementCompanyName = company.Value.CompanyName,
                    CurrentLevel = WorkspaceLevel.ManagementCompany
                },
                cancellationToken),
            CompanySlug = company.Value.CompanySlug,
            CompanyName = company.Value.CompanyName,
            Customers = _mapper.ToListItems(listResult.Value),
            AddCustomer = addCustomerOverride ?? new AddManagementCustomerViewModel()
        });
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

    private Guid? GetAppUserId()
    {
        return _portalContextResolver.Resolve().AppUserId;
    }

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
