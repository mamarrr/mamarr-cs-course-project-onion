using System.Security.Claims;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.PropertyWorkspace.Properties;
using App.BLL.UnitWorkspace.Units;
using App.BLL.UnitWorkspace.Workspace;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services.SharedLayout;
using WebApp.ViewModels.Management.CustomerProperties;
using WebApp.ViewModels.Property.PropertyUnits;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.Areas.Property.Controllers;

[Area("Property")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}/p/{propertySlug}/units")]
public class PropertyUnitsController : Controller
{
    private readonly IManagementCustomerAccessService _managementCustomerAccessService;
    private readonly IManagementCustomerPropertyService _managementCustomerPropertyService;
    private readonly IManagementPropertyUnitService _managementPropertyUnitService;
    private readonly IWorkspaceLayoutContextProvider _workspaceLayoutContextProvider;

    public PropertyUnitsController(
        IManagementCustomerAccessService managementCustomerAccessService,
        IManagementCustomerPropertyService managementCustomerPropertyService,
        IManagementPropertyUnitService managementPropertyUnitService,
        IWorkspaceLayoutContextProvider workspaceLayoutContextProvider)
    {
        _managementCustomerAccessService = managementCustomerAccessService;
        _managementCustomerPropertyService = managementCustomerPropertyService;
        _managementPropertyUnitService = managementPropertyUnitService;
        _workspaceLayoutContextProvider = workspaceLayoutContextProvider;
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
        PropertyUnitsPageViewModel vm,
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

        var createResult = await _managementPropertyUnitService.CreateUnitAsync(
            access.context!,
            new ManagementPropertyUnitCreateRequest
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
                    nameof(PropertyUnitsPageViewModel.AddUnit) + "." + nameof(AddUnitViewModel.UnitNr),
                    createResult.ErrorMessage ?? T("RequiredField", "The {0} field is required.").Replace("{0}", T("UnitNr", "Unit number")));
            }
            else if (createResult.InvalidFloorNr)
            {
                ModelState.AddModelError(
                    nameof(PropertyUnitsPageViewModel.AddUnit) + "." + nameof(AddUnitViewModel.FloorNr),
                    createResult.ErrorMessage ?? T("InvalidData", "Invalid Data."));
            }
            else if (createResult.InvalidSizeM2)
            {
                ModelState.AddModelError(
                    nameof(PropertyUnitsPageViewModel.AddUnit) + "." + nameof(AddUnitViewModel.SizeM2),
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

    private async Task<(IActionResult? response, ManagementCustomerPropertyDashboardContext? context)> ResolvePropertyContextAsync(
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

        var customerAccess = await _managementCustomerAccessService.ResolveDashboardAccessAsync(
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

        var propertyAccess = await _managementCustomerPropertyService.ResolvePropertyDashboardContextAsync(
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

    private async Task<PropertyUnitsPageViewModel> BuildUnitsPageViewModelAsync(
        ManagementCustomerPropertyDashboardContext context,
        CancellationToken cancellationToken,
        AddUnitViewModel? addUnitOverride = null)
    {
        var listResult = await _managementPropertyUnitService.ListUnitsAsync(context, cancellationToken);
        var propertyLayout = new PropertyLayoutViewModel
        {
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            CurrentSection = "Units"
        };

        var pageShell = await BuildPageShellAsync(
            UiText.Units,
            UiText.Units,
            propertyLayout.CompanySlug,
            cancellationToken,
            propertyLayout);

        return new PropertyUnitsPageViewModel
        {
            PageShell = pageShell,
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

    private async Task<PropertyPageShellViewModel> BuildPageShellAsync(
        string title,
        string currentSectionLabel,
        string companySlug,
        CancellationToken cancellationToken,
        PropertyLayoutViewModel propertyLayout)
    {
        var layoutContext = await _workspaceLayoutContextProvider.BuildAsync(
            User,
            BuildWorkspaceRequest(companySlug),
            cancellationToken);

        return new PropertyPageShellViewModel
        {
            Title = title,
            CurrentSectionLabel = currentSectionLabel,
            LayoutContext = layoutContext,
            Property = propertyLayout
        };
    }

    private WorkspaceLayoutRequestViewModel BuildWorkspaceRequest(string companySlug)
    {
        return new WorkspaceLayoutRequestViewModel
        {
            CurrentController = ControllerContext.ActionDescriptor.ControllerName,
            CompanySlug = companySlug,
            CurrentPathAndQuery = $"{Request.Path}{Request.QueryString}",
            CurrentUiCultureName = Thread.CurrentThread.CurrentUICulture.Name
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
