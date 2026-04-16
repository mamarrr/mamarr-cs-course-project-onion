using System.Security.Claims;
using App.BLL.Management;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebApp.Services.SharedLayout;
using WebApp.ViewModels.Shared.Layout;
using WebApp.ViewModels.Unit;

namespace WebApp.Areas.Unit.Controllers;

[Area("Unit")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}/p/{propertySlug}/u/{unitSlug}/tenants")]
public class UnitTenantsController : Controller
{
    private const string SuccessTempDataKey = "UnitTenantsSuccess";
    private const string ErrorTempDataKey = "UnitTenantsError";

    private readonly IManagementCustomerAccessService _managementCustomerAccessService;
    private readonly IManagementCustomerPropertyService _managementCustomerPropertyService;
    private readonly IManagementUnitDashboardService _managementUnitDashboardService;
    private readonly IManagementLeaseService _managementLeaseService;
    private readonly IManagementLeaseSearchService _managementLeaseSearchService;
    private readonly IWorkspaceLayoutContextProvider _workspaceLayoutContextProvider;

    public UnitTenantsController(
        IManagementCustomerAccessService managementCustomerAccessService,
        IManagementCustomerPropertyService managementCustomerPropertyService,
        IManagementUnitDashboardService managementUnitDashboardService,
        IManagementLeaseService managementLeaseService,
        IManagementLeaseSearchService managementLeaseSearchService,
        IWorkspaceLayoutContextProvider workspaceLayoutContextProvider)
    {
        _managementCustomerAccessService = managementCustomerAccessService;
        _managementCustomerPropertyService = managementCustomerPropertyService;
        _managementUnitDashboardService = managementUnitDashboardService;
        _managementLeaseService = managementLeaseService;
        _managementLeaseSearchService = managementLeaseSearchService;
        _workspaceLayoutContextProvider = workspaceLayoutContextProvider;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        [FromQuery] UnitTenantsPageViewModel? query,
        CancellationToken cancellationToken)
    {
        var access = await ResolveUnitContextAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var vm = await BuildPageViewModelAsync(access.context!, cancellationToken, addOverride: query?.AddLease, requestedEditLeaseId: query?.ActiveEditLeaseId);
        return View("~/Areas/Unit/Views/UnitTenants/Index.cshtml", vm);
    }

    [HttpGet("resident-search")]
    public async Task<IActionResult> SearchResidents(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        [FromQuery] string? searchTerm,
        CancellationToken cancellationToken)
    {
        var access = await ResolveUnitContextAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var result = await _managementLeaseSearchService.SearchResidentsAsync(access.context!, searchTerm, cancellationToken);

        return Json(result.Residents.Select(x => new UnitLeaseResidentSearchResultViewModel
        {
            ResidentId = x.ResidentId,
            FullName = x.FullName,
            IdCode = x.IdCode,
            IsActive = x.IsActive
        }));
    }

    [HttpPost("leases/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        UnitTenantsPageViewModel vm,
        CancellationToken cancellationToken)
    {
        var access = await ResolveUnitContextAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        foreach (var key in ModelState.Keys
                     .Where(k => k.StartsWith($"{nameof(UnitTenantsPageViewModel.EditLease)}.", StringComparison.Ordinal))
                     .ToList())
        {
            ModelState.Remove(key);
        }

        if (!TryValidateModel(vm.AddLease, nameof(UnitTenantsPageViewModel.AddLease)))
        {
            var invalidVm = await BuildPageViewModelAsync(access.context!, cancellationToken, addOverride: vm.AddLease);
            return View("~/Areas/Unit/Views/UnitTenants/Index.cshtml", invalidVm);
        }

        var result = await _managementLeaseService.CreateFromUnitAsync(
            access.context!,
            new ManagementLeaseCreateRequest
            {
                ResidentId = vm.AddLease.ResidentId!.Value,
                UnitId = access.context!.UnitId,
                LeaseRoleId = vm.AddLease.LeaseRoleId!.Value,
                StartDate = DateOnly.FromDateTime(vm.AddLease.StartDate),
                EndDate = vm.AddLease.EndDate.HasValue ? DateOnly.FromDateTime(vm.AddLease.EndDate.Value) : null,
                IsActive = vm.AddLease.IsActive,
                Notes = vm.AddLease.Notes
            },
            cancellationToken);

        if (!result.Success)
        {
            ApplyCreateErrors(result, ModelState);
            var invalidVm = await BuildPageViewModelAsync(access.context!, cancellationToken, addOverride: vm.AddLease);
            return View("~/Areas/Unit/Views/UnitTenants/Index.cshtml", invalidVm);
        }

        TempData[SuccessTempDataKey] = T("LeaseAddedSuccessfully", "Lease added successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, customerSlug, propertySlug, unitSlug });
    }

    [HttpPost("leases/{leaseId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        Guid leaseId,
        UnitTenantsPageViewModel vm,
        CancellationToken cancellationToken)
    {
        var access = await ResolveUnitContextAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var editVm = vm.EditLease;
        editVm.LeaseId = leaseId;

        foreach (var key in ModelState.Keys
                     .Where(k => k.StartsWith($"{nameof(UnitTenantsPageViewModel.AddLease)}.", StringComparison.Ordinal))
                     .ToList())
        {
            ModelState.Remove(key);
        }

        if (!TryValidateModel(editVm, nameof(UnitTenantsPageViewModel.EditLease)))
        {
            var invalidVm = await BuildPageViewModelAsync(access.context!, cancellationToken, editOverride: editVm, requestedEditLeaseId: leaseId);
            invalidVm.ActiveEditLeaseId = leaseId;
            return View("~/Areas/Unit/Views/UnitTenants/Index.cshtml", invalidVm);
        }

        var result = await _managementLeaseService.UpdateFromUnitAsync(
            access.context!,
            new ManagementLeaseUpdateRequest
            {
                LeaseId = leaseId,
                LeaseRoleId = editVm.LeaseRoleId!.Value,
                StartDate = DateOnly.FromDateTime(editVm.StartDate),
                EndDate = editVm.EndDate.HasValue ? DateOnly.FromDateTime(editVm.EndDate.Value) : null,
                IsActive = editVm.IsActive,
                Notes = editVm.Notes
            },
            cancellationToken);

        if (!result.Success)
        {
            ApplyEditErrors(result, ModelState);
            var invalidVm = await BuildPageViewModelAsync(access.context!, cancellationToken, editOverride: editVm, requestedEditLeaseId: leaseId);
            invalidVm.ActiveEditLeaseId = leaseId;
            return View("~/Areas/Unit/Views/UnitTenants/Index.cshtml", invalidVm);
        }

        TempData[SuccessTempDataKey] = T("LeaseUpdatedSuccessfully", "Lease updated successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, customerSlug, propertySlug, unitSlug });
    }

    [HttpPost("leases/{leaseId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        var access = await ResolveUnitContextAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var result = await _managementLeaseService.DeleteFromUnitAsync(
            access.context!,
            new ManagementLeaseDeleteRequest
            {
                LeaseId = leaseId
            },
            cancellationToken);

        if (!result.Success)
        {
            TempData[ErrorTempDataKey] = result.ErrorMessage ?? T("UnableToDeleteLease", "Unable to delete lease.");
            return RedirectToAction(nameof(Index), new { companySlug, customerSlug, propertySlug, unitSlug });
        }

        TempData[SuccessTempDataKey] = T("LeaseDeletedSuccessfully", "Lease deleted successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, customerSlug, propertySlug, unitSlug });
    }

    private async Task<(IActionResult? response, ManagementUnitDashboardContext? context)> ResolveUnitContextAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
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

        var unitAccess = await _managementUnitDashboardService.ResolveUnitDashboardContextAsync(
            propertyAccess.Context,
            unitSlug,
            cancellationToken);

        if (unitAccess.UnitNotFound)
        {
            return (NotFound(), null);
        }

        if (!unitAccess.IsAuthorized || unitAccess.Context == null)
        {
            return (Forbid(), null);
        }

        return (null, unitAccess.Context);
    }

    private async Task<UnitTenantsPageViewModel> BuildPageViewModelAsync(
        ManagementUnitDashboardContext context,
        CancellationToken cancellationToken,
        AddUnitLeaseViewModel? addOverride = null,
        EditUnitLeaseViewModel? editOverride = null,
        Guid? requestedEditLeaseId = null)
    {
        var leaseList = await _managementLeaseService.ListForUnitAsync(context, cancellationToken);
        var roleOptions = await _managementLeaseSearchService.ListLeaseRolesAsync(cancellationToken);
        var residentSearchTerm = addOverride?.ResidentSearchTerm;
        var residentResults = string.IsNullOrWhiteSpace(residentSearchTerm)
            ? Array.Empty<ManagementLeaseResidentSearchItem>()
            : (await _managementLeaseSearchService.SearchResidentsAsync(context, residentSearchTerm, cancellationToken)).Residents;

        var unitLayout = new UnitLayoutViewModel
        {
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitSlug = context.UnitSlug,
            UnitName = context.UnitNr,
            CurrentSection = "Tenants"
        };

        var pageShell = await BuildPageShellAsync(
            T("Tenants", "Tenants"),
            T("Tenants", "Tenants"),
            context.CompanySlug,
            cancellationToken,
            unitLayout);

        var leases = leaseList.Leases.Select(x => new UnitTenantLeaseListItemViewModel
        {
            LeaseId = x.LeaseId,
            ResidentId = x.ResidentId,
            ResidentFullName = x.ResidentFullName,
            ResidentIdCode = x.ResidentIdCode,
            LeaseRoleId = x.LeaseRoleId,
            LeaseRoleLabel = x.LeaseRoleLabel,
            StartDate = x.StartDate.ToDateTime(TimeOnly.MinValue),
            EndDate = x.EndDate?.ToDateTime(TimeOnly.MinValue),
            IsActive = x.IsActive,
            Notes = x.Notes
        }).ToList();

        var activeEditLeaseId = requestedEditLeaseId;
        var editLease = editOverride ?? new EditUnitLeaseViewModel();
        if (activeEditLeaseId.HasValue && editOverride == null)
        {
            var selectedLease = leases.FirstOrDefault(x => x.LeaseId == activeEditLeaseId.Value);
            if (selectedLease != null)
            {
                editLease = new EditUnitLeaseViewModel
                {
                    LeaseId = selectedLease.LeaseId,
                    LeaseRoleId = selectedLease.LeaseRoleId,
                    StartDate = selectedLease.StartDate,
                    EndDate = selectedLease.EndDate,
                    IsActive = selectedLease.IsActive,
                    Notes = selectedLease.Notes
                };
            }
        }

        return new UnitTenantsPageViewModel
        {
            PageShell = pageShell,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitSlug = context.UnitSlug,
            UnitName = context.UnitNr,
            SuccessMessage = TempData[SuccessTempDataKey] as string,
            ErrorMessage = TempData[ErrorTempDataKey] as string,
            ActiveEditLeaseId = activeEditLeaseId,
            Leases = leases,
            LeaseRoleOptions = roleOptions.Roles.Select(x => new UnitLeaseRoleOptionViewModel
            {
                LeaseRoleId = x.LeaseRoleId,
                Label = x.Label
            }).ToList(),
            ResidentSearchResults = residentResults.Select(x => new UnitLeaseResidentSearchResultViewModel
            {
                ResidentId = x.ResidentId,
                FullName = x.FullName,
                IdCode = x.IdCode,
                IsActive = x.IsActive
            }).ToList(),
            AddLease = addOverride ?? new AddUnitLeaseViewModel(),
            EditLease = editLease
        };
    }

    private async Task<UnitPageShellViewModel> BuildPageShellAsync(
        string title,
        string currentSectionLabel,
        string companySlug,
        CancellationToken cancellationToken,
        UnitLayoutViewModel unitLayout)
    {
        var layoutContext = await _workspaceLayoutContextProvider.BuildAsync(
            User,
            BuildWorkspaceRequest(companySlug),
            cancellationToken);

        return new UnitPageShellViewModel
        {
            Title = title,
            CurrentSectionLabel = currentSectionLabel,
            LayoutContext = layoutContext,
            Unit = unitLayout
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

    private static void ApplyCreateErrors(ManagementLeaseCommandResult result, ModelStateDictionary modelState)
    {
        if (result.ResidentNotFound)
        {
            modelState.AddModelError("AddLease.ResidentId", result.ErrorMessage ?? T("UnableToLoadResident", "Unable to load resident."));
            return;
        }

        if (result.UnitNotFound)
        {
            modelState.AddModelError(string.Empty, result.ErrorMessage ?? UiText.UnableToLoadUnit);
            return;
        }

        if (result.InvalidLeaseRole)
        {
            modelState.AddModelError("AddLease.LeaseRoleId", result.ErrorMessage ?? T("InvalidLeaseRole", "Selected lease role is invalid."));
            return;
        }

        if (result.InvalidStartDate)
        {
            modelState.AddModelError("AddLease.StartDate", result.ErrorMessage ?? UiText.RequiredField);
            return;
        }

        if (result.InvalidEndDate)
        {
            modelState.AddModelError("AddLease.EndDate", result.ErrorMessage ?? T("InvalidEndDate", "End date must be on or after the start date."));
            return;
        }

        if (result.DuplicateActiveLease)
        {
            modelState.AddModelError(string.Empty, result.ErrorMessage ?? T("DuplicateActiveLease", "An active lease for this resident already exists for the unit."));
            return;
        }

        modelState.AddModelError(string.Empty, result.ErrorMessage ?? T("UnableToAddLease", "Unable to add lease."));
    }

    private static void ApplyEditErrors(ManagementLeaseCommandResult result, ModelStateDictionary modelState)
    {
        if (result.LeaseNotFound)
        {
            modelState.AddModelError(string.Empty, result.ErrorMessage ?? T("UnableToLoadLease", "Unable to load lease."));
            return;
        }

        if (result.InvalidLeaseRole)
        {
            modelState.AddModelError("EditLease.LeaseRoleId", result.ErrorMessage ?? T("InvalidLeaseRole", "Selected lease role is invalid."));
            return;
        }

        if (result.InvalidStartDate)
        {
            modelState.AddModelError("EditLease.StartDate", result.ErrorMessage ?? UiText.RequiredField);
            return;
        }

        if (result.InvalidEndDate)
        {
            modelState.AddModelError("EditLease.EndDate", result.ErrorMessage ?? T("InvalidEndDate", "End date must be on or after the start date."));
            return;
        }

        if (result.DuplicateActiveLease)
        {
            modelState.AddModelError(string.Empty, result.ErrorMessage ?? T("DuplicateActiveLease", "An active lease for this resident already exists for the unit."));
            return;
        }

        modelState.AddModelError(string.Empty, result.ErrorMessage ?? T("UnableToUpdateLease", "Unable to update lease."));
    }

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
