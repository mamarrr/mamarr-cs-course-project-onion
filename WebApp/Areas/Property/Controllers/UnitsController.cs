using System.Security.Claims;
using App.BLL.Contracts.Customers.Services;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.PropertyWorkspace.Properties;
using App.BLL.UnitWorkspace.Units;
using App.BLL.UnitWorkspace.Workspace;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Property;

namespace WebApp.Areas.Property.Controllers;

[Area("Property")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}/p/{propertySlug}/units")]
public class UnitsController : Controller
{
    private readonly ICustomerAccessService _customerAccessService;
    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly IPropertyUnitService _propertyUnitService;
    private readonly IAppChromeBuilder _appChromeBuilder;

    public UnitsController(
        ICustomerAccessService customerAccessService,
        IPropertyWorkspaceService propertyWorkspaceService,
        IPropertyUnitService propertyUnitService,
        IAppChromeBuilder appChromeBuilder)
    {
        _customerAccessService = customerAccessService;
        _propertyWorkspaceService = propertyWorkspaceService;
        _propertyUnitService = propertyUnitService;
        _appChromeBuilder = appChromeBuilder;
    }

    [HttpGet("")]
    public async Task<IActionResult> Units(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        var access = await ResolvePropertyContextAsync(companySlug, customerSlug, propertySlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var vm = await BuildUnitsPageViewModelAsync(access.context!, cancellationToken);
        return View("~/Areas/Property/Views/Units/Index.cshtml", vm);
    }

    [HttpPost("/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUnit(
        string companySlug,
        string customerSlug,
        string propertySlug,
        UnitsPageViewModel vm,
        CancellationToken cancellationToken)
    {
        var access = await ResolvePropertyContextAsync(companySlug, customerSlug, propertySlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildUnitsPageViewModelAsync(access.context!, cancellationToken, vm.AddUnit);
            return View("~/Areas/Property/Views/Units/Index.cshtml", invalidVm);
        }

        var createResult = await _propertyUnitService.CreateUnitAsync(
            access.context!,
            new UnitCreateRequest
            {
                UnitNr = vm.AddUnit.UnitNr,
                FloorNr = vm.AddUnit.FloorNr,
                SizeM2 = vm.AddUnit.SizeM2,
                Notes = vm.AddUnit.Notes
            },
            cancellationToken);

        if (!createResult.Success)
        {
            if (createResult.InvalidUnitNr)
            {
                ModelState.AddModelError(
                    nameof(UnitsPageViewModel.AddUnit) + "." + nameof(AddUnitViewModel.UnitNr),
                    createResult.ErrorMessage ?? T("RequiredField", "The {0} field is required.").Replace("{0}", T("UnitNr", "Unit number")));
            }
            else if (createResult.InvalidFloorNr)
            {
                ModelState.AddModelError(
                    nameof(UnitsPageViewModel.AddUnit) + "." + nameof(AddUnitViewModel.FloorNr),
                    createResult.ErrorMessage ?? T("InvalidData", "Invalid Data."));
            }
            else if (createResult.InvalidSizeM2)
            {
                ModelState.AddModelError(
                    nameof(UnitsPageViewModel.AddUnit) + "." + nameof(AddUnitViewModel.SizeM2),
                    createResult.ErrorMessage ?? T("InvalidData", "Invalid Data."));
            }
            else
            {
                ModelState.AddModelError(
                    string.Empty,
                    createResult.ErrorMessage ?? T("ErrorOccurred", "An error occurred while processing your request."));
            }

            var invalidVm = await BuildUnitsPageViewModelAsync(access.context!, cancellationToken, vm.AddUnit);
            return View("~/Areas/Property/Views/Units/Index.cshtml", invalidVm);
        }

        TempData["PropertyUnitsSuccess"] = T("UnitAddedSuccessfully", "Unit added successfully.");
        return RedirectToAction(nameof(Units), new { companySlug, customerSlug, propertySlug });
    }

    private async Task<(IActionResult? response, PropertyDashboardContext? context)> ResolvePropertyContextAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (Challenge(), null);
        }

        var customerAccess = await _customerAccessService.ResolveDashboardAccessAsync(
            appUserId.Value,
            companySlug,
            customerSlug,
            cancellationToken);

        if (customerAccess.CompanyNotFound || customerAccess.CustomerNotFound)
        {
            return (NotFound(), null);
        }

        if (customerAccess.IsForbidden || customerAccess.Context == null)
        {
            return (Forbid(), null);
        }

        var propertyAccess = await _propertyWorkspaceService.ResolvePropertyDashboardContextAsync(
            customerAccess.Context,
            propertySlug,
            cancellationToken);

        if (propertyAccess.PropertyNotFound)
        {
            return (NotFound(), null);
        }

        if (!propertyAccess.IsAuthorized || propertyAccess.Context == null)
        {
            return (Forbid(), null);
        }

        return (null, propertyAccess.Context);
    }

    private async Task<UnitsPageViewModel> BuildUnitsPageViewModelAsync(
        PropertyDashboardContext context,
        CancellationToken cancellationToken,
        AddUnitViewModel? addUnitOverride = null)
    {
        var listResult = await _propertyUnitService.ListUnitsAsync(context, cancellationToken);

        return new UnitsPageViewModel
        {
            AppChrome = await BuildAppChromeAsync(context, UiText.Units, cancellationToken),
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            Units = listResult.Units.Select(x => new PropertyUnitListItemViewModel
            {
                UnitId = x.UnitId,
                UnitSlug = x.UnitSlug,
                UnitNr = x.UnitNr,
                FloorNr = x.FloorNr,
                SizeM2 = x.SizeM2
            }).ToList(),
            AddUnit = addUnitOverride ?? new AddUnitViewModel()
        };
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        PropertyDashboardContext context,
        string title,
        CancellationToken cancellationToken)
    {
        return _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = title,
                ActiveSection = Sections.Units,
                ManagementCompanySlug = context.CompanySlug,
                ManagementCompanyName = context.CompanyName,
                CustomerSlug = context.CustomerSlug,
                CustomerName = context.CustomerName,
                PropertySlug = context.PropertySlug,
                PropertyName = context.PropertyName,
                CurrentLevel = WorkspaceLevel.Property
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
