using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Customers;
using App.BLL.Contracts.Customers.Queries;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers.Mvc.Customers;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.Customers;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
[Authorize]
[Route("m/{companySlug}/customers")]
public class CustomersController : Controller
{
    private readonly ICompanyCustomerService _companyCustomerService;
    private readonly ICustomerAccessService _customerAccessService;
    private readonly CompanyCustomerMvcMapper _mapper;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        ICompanyCustomerService companyCustomerService,
        ICustomerAccessService customerAccessService,
        CompanyCustomerMvcMapper mapper,
        IAppChromeBuilder appChromeBuilder,
        ILogger<CustomersController> logger)
    {
        _companyCustomerService = companyCustomerService;
        _customerAccessService = customerAccessService;
        _mapper = mapper;
        _appChromeBuilder = appChromeBuilder;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string companySlug, CancellationToken cancellationToken)
    {
        var query = _mapper.ToQuery(companySlug, User);
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
                _mapper.ToQuery(companySlug, User),
                cancellationToken,
                vm.AddCustomer);
            if (invalidVm.response is not null)
            {
                return invalidVm.response;
            }

            return View(nameof(Index), invalidVm.model);
        }

        var createResult = await _companyCustomerService.CreateCustomerAsync(
            _mapper.ToCommand(companySlug, vm.AddCustomer, User),
            cancellationToken);

        if (createResult.IsFailed)
        {
            var duplicateRegistryCode = createResult.Errors.Any(error => error is App.BLL.Contracts.Customers.Errors.DuplicateRegistryCodeError);
            var invalidBillingEmail = createResult.Errors
                .OfType<App.BLL.Contracts.Common.Errors.ValidationAppError>()
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
                _mapper.ToQuery(companySlug, User),
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
        var company = await _customerAccessService.ResolveCompanyWorkspaceAsync(query, cancellationToken);
        if (company.IsFailed)
        {
            return (ToMvcErrorResult(company.Errors), null);
        }

        var listResult = await _companyCustomerService.GetCompanyCustomersAsync(query, cancellationToken);
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

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
