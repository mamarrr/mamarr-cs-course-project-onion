using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.Properties;
using App.BLL.DTO.Properties.Commands;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebApp.Mappers.Mvc.Customers;
using WebApp.Mappers.Mvc.Properties;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Customer.CustomerProperties;

namespace WebApp.Areas.Portal.Controllers.Customer;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/customers/{customerSlug}/properties")]
public class CustomerPropertiesController : Controller
{
    private readonly IAppBLL _bll;
    private readonly CustomerWorkspaceMvcMapper _customerMapper;
    private readonly PropertyMvcMapper _propertyMapper;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;
    private readonly ILogger<CustomerPropertiesController> _logger;

    public CustomerPropertiesController(
        IAppBLL bll,
        CustomerWorkspaceMvcMapper customerMapper,
        PropertyMvcMapper propertyMapper,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver,
        ILogger<CustomerPropertiesController> logger)
    {
        _bll = bll;
        _customerMapper = customerMapper;
        _propertyMapper = propertyMapper;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
        _logger = logger;
    }

    [HttpGet("", Name = PortalRouteNames.CustomerProperties)]
    public async Task<IActionResult> Index(string companySlug, string customerSlug, CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var customer = await ResolveCustomerAsync(companySlug, customerSlug, appUserId.Value, cancellationToken);
        if (customer.response is not null)
        {
            return customer.response;
        }

        var vm = await BuildPageViewModelAsync(customer.context!, companySlug, customerSlug, appUserId.Value, cancellationToken);
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
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var customer = await ResolveCustomerAsync(companySlug, customerSlug, appUserId.Value, cancellationToken);
        if (customer.response is not null)
        {
            return customer.response;
        }

        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildPageViewModelAsync(customer.context!, companySlug, customerSlug, appUserId.Value, cancellationToken, vm.AddProperty);
            return View(nameof(Index), invalidVm);
        }

        var createResult = await _bll.Properties.CreateAndGetProfileAsync(
            ToCustomerRoute(companySlug, customerSlug, appUserId.Value),
            ToPropertyDto(vm.AddProperty),
            cancellationToken);

        if (createResult.IsFailed)
        {
            ApplyErrors(createResult.Errors, ModelState);
            var invalidVm = await BuildPageViewModelAsync(customer.context!, companySlug, customerSlug, appUserId.Value, cancellationToken, vm.AddProperty);
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
        Guid appUserId,
        CancellationToken cancellationToken)
    {
        var result = await _bll.Customers.GetWorkspaceAsync(
            ToCustomerRoute(companySlug, customerSlug, appUserId),
            cancellationToken);

        return result.IsFailed
            ? (ToMvcErrorResult(result.Errors), null)
            : (null, result.Value);
    }

    private async Task<PropertiesPageViewModel> BuildPageViewModelAsync(
        CustomerWorkspaceModel context,
        string companySlug,
        string customerSlug,
        Guid appUserId,
        CancellationToken cancellationToken,
        AddPropertyViewModel? addPropertyOverride = null)
    {
        var listResult = await _bll.Properties.ListForCustomerAsync(
            ToCustomerRoute(companySlug, customerSlug, appUserId),
            cancellationToken);

        var propertyTypeResult = await _bll.Properties.GetPropertyTypeOptionsAsync(cancellationToken);

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
                var key = failure.PropertyName == nameof(CreatePropertyCommand.PropertyTypeId)
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

    private Guid? GetAppUserId()
    {
        return _portalContextResolver.Resolve().AppUserId;
    }

    private static CustomerRoute ToCustomerRoute(string companySlug, string customerSlug, Guid appUserId)
    {
        return new CustomerRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug,
            CustomerSlug = customerSlug
        };
    }

    private static PropertyBllDto ToPropertyDto(AddPropertyViewModel vm)
    {
        return new PropertyBllDto
        {
            Label = vm.Name,
            AddressLine = vm.AddressLine,
            City = vm.City,
            PostalCode = vm.PostalCode,
            PropertyTypeId = vm.PropertyTypeId ?? Guid.Empty,
            Notes = vm.Notes
        };
    }

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
