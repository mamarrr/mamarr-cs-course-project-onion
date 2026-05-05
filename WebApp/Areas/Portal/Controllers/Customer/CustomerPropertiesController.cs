using App.BLL.Contracts;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Customers.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebApp.Mappers.Api.Customers;
using WebApp.Mappers.Mvc.Properties;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Customer.CustomerProperties;

namespace WebApp.Areas.Portal.Controllers.Customer;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/customers/{customerSlug}/properties")]
public class CustomerPropertiesController : Controller
{
    private readonly IAppBLL _bll;
    private readonly CustomerWorkspaceApiMapper _customerMapper;
    private readonly PropertyMvcMapper _propertyMapper;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ILogger<CustomerPropertiesController> _logger;

    public CustomerPropertiesController(
        IAppBLL bll,
        CustomerWorkspaceApiMapper customerMapper,
        PropertyMvcMapper propertyMapper,
        IAppChromeBuilder appChromeBuilder,
        ILogger<CustomerPropertiesController> logger)
    {
        _bll = bll;
        _customerMapper = customerMapper;
        _propertyMapper = propertyMapper;
        _appChromeBuilder = appChromeBuilder;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string companySlug, string customerSlug, CancellationToken cancellationToken)
    {
        var customer = await ResolveCustomerAsync(companySlug, customerSlug, cancellationToken);
        if (customer.response is not null)
        {
            return customer.response;
        }

        var vm = await BuildPageViewModelAsync(customer.context!, companySlug, customerSlug, cancellationToken);
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
        var customer = await ResolveCustomerAsync(companySlug, customerSlug, cancellationToken);
        if (customer.response is not null)
        {
            return customer.response;
        }

        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildPageViewModelAsync(customer.context!, companySlug, customerSlug, cancellationToken, vm.AddProperty);
            return View(nameof(Index), invalidVm);
        }

        var createResult = await _bll.PropertyWorkspaces.CreateAsync(
            _propertyMapper.ToCreateCommand(companySlug, customerSlug, vm.AddProperty, User),
            cancellationToken);

        if (createResult.IsFailed)
        {
            ApplyErrors(createResult.Errors, ModelState);
            var invalidVm = await BuildPageViewModelAsync(customer.context!, companySlug, customerSlug, cancellationToken, vm.AddProperty);
            return View(nameof(Index), invalidVm);
        }

        _logger.LogInformation(
            "Customer property add succeeded. CompanySlug={CompanySlug}, CustomerSlug={CustomerSlug}, CreatedPropertyId={CreatedPropertyId}, CreatedPropertySlug={CreatedPropertySlug}",
            companySlug,
            customerSlug,
            createResult.Value.PropertyId,
            createResult.Value.PropertySlug);

        TempData["CustomerPropertiesSuccess"] = T("PropertyAddedSuccessfully", "Property added successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, customerSlug });
    }

    private async Task<(IActionResult? response, CustomerWorkspaceModel? context)> ResolveCustomerAsync(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var result = await _bll.CustomerWorkspaces.GetWorkspaceAsync(
            _customerMapper.ToQuery(companySlug, customerSlug, User),
            cancellationToken);

        return result.IsFailed
            ? (ToMvcErrorResult(result.Errors), null)
            : (null, result.Value);
    }

    private async Task<PropertiesPageViewModel> BuildPageViewModelAsync(
        CustomerWorkspaceModel context,
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken,
        AddPropertyViewModel? addPropertyOverride = null)
    {
        var listResult = await _bll.PropertyWorkspaces.GetCustomerPropertiesAsync(
            _propertyMapper.ToCustomerPropertiesQuery(companySlug, customerSlug, User),
            cancellationToken);

        var propertyTypeResult = await _bll.PropertyWorkspaces.GetPropertyTypeOptionsAsync(cancellationToken);

        return new PropertiesPageViewModel
        {
            AppChrome = await BuildAppChromeAsync(context, UiText.Properties, cancellationToken),
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            Properties = listResult.IsSuccess
                ? _propertyMapper.ToCustomerPropertyListItems(listResult.Value)
                : Array.Empty<CustomerPropertyListItemViewModel>(),
            AddProperty = addPropertyOverride ?? new AddPropertyViewModel(),
            PropertyTypeOptions = propertyTypeResult.IsSuccess
                ? _propertyMapper.ToPropertyTypeOptions(propertyTypeResult.Value)
                : Array.Empty<PropertyTypeOptionViewModel>()
        };
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        CustomerWorkspaceModel context,
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

    private IActionResult ToMvcErrorResult(IReadOnlyList<IError> errors)
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

    private static void ApplyErrors(IReadOnlyList<IError> errors, ModelStateDictionary modelState)
    {
        var validation = errors.OfType<ValidationAppError>().FirstOrDefault();
        if (validation is not null)
        {
            foreach (var failure in validation.Failures)
            {
                var key = failure.PropertyName == nameof(App.BLL.Contracts.Properties.Commands.CreatePropertyCommand.PropertyTypeId)
                    ? "AddProperty.PropertyTypeId"
                    : failure.PropertyName is null
                        ? string.Empty
                        : $"AddProperty.{failure.PropertyName}";

                modelState.AddModelError(key, failure.ErrorMessage);
            }

            return;
        }

        modelState.AddModelError(string.Empty, errors.FirstOrDefault()?.Message ?? T("UnableToAddProperty", "Unable to add property."));
    }

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
