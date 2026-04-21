using System.Security.Claims;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.LeaseAssignments;
using App.BLL.PropertyWorkspace.Properties;
using App.BLL.UnitWorkspace.Access;
using App.BLL.UnitWorkspace.Workspace;
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
public class TenantsController : Controller
{
    private const string SuccessTempDataKey = "UnitTenantsSuccess";
    private const string ErrorTempDataKey = "UnitTenantsError";

    private readonly ICustomerAccessService _customerAccessService;
    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly IUnitAccessService _unitAccessService;
    private readonly ILeaseAssignmentService _leaseAssignmentService;
    private readonly ILeaseLookupService _leaseLookupService;
    private readonly IWorkspaceLayoutContextProvider _workspaceLayoutContextProvider;

    public TenantsController(
        ICustomerAccessService customerAccessService,
        IPropertyWorkspaceService propertyWorkspaceService,
        IUnitAccessService unitAccessService,
        ILeaseAssignmentService leaseAssignmentService,
        ILeaseLookupService leaseLookupService,
        IWorkspaceLayoutContextProvider workspaceLayoutContextProvider)
    {
        _customerAccessService = customerAccessService;
        _propertyWorkspaceService = propertyWorkspaceService;
        _unitAccessService = unitAccessService;
        _leaseAssignmentService = leaseAssignmentService;
        _leaseLookupService = leaseLookupService;
        _workspaceLayoutContextProvider = workspaceLayoutContextProvider;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        [FromQuery] TenantsPageViewModel? query,
        CancellationToken cancellationToken)
    {
        var access = await ResolveUnitContextAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var vm = await BuildPageViewModelAsync(access.context!, cancellationToken, addOverride: query?.AddLease, requestedEditLeaseId: query?.ActiveEditLeaseId);
        return View("~/Areas/Unit/Views/Tenants/Index.cshtml", vm);
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

        var result = await _leaseLookupService.SearchResidentsAsync(access.context!, searchTerm, cancellationToken);

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
        TenantsPageViewModel vm,
        CancellationToken cancellationToken)
    {
        var access = await ResolveUnitContextAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        foreach (var key in ModelState.Keys
                     .Where(k => k.StartsWith($"{nameof(TenantsPageViewModel.EditLease)}.", StringComparison.Ordinal))
                     .ToList())
        {
            ModelState.Remove(key);
        }

        if (!TryValidateModel(vm.AddLease, nameof(TenantsPageViewModel.AddLease)))
        {
            var invalidVm = await BuildPageViewModelAsync(access.context!, cancellationToken, addOverride: vm.AddLease);
            return View("~/Areas/Unit/Views/Tenants/Index.cshtml", invalidVm);
        }

        var result = await _leaseAssignmentService.CreateFromUnitAsync(
            access.context!,
            new LeaseCreateRequest
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
            return View("~/Areas/Unit/Views/Tenants/Index.cshtml", invalidVm);
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
        TenantsPageViewModel vm,
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
                     .Where(k => k.StartsWith($"{nameof(TenantsPageViewModel.AddLease)}.", StringComparison.Ordinal))
                     .ToList())
        {
            ModelState.Remove(key);
        }

        if (!TryValidateModel(editVm, nameof(TenantsPageViewModel.EditLease)))
        {
            var invalidVm = await BuildPageViewModelAsync(access.context!, cancellationToken, editOverride: editVm, requestedEditLeaseId: leaseId);
            invalidVm.ActiveEditLeaseId = leaseId;
            return View("~/Areas/Unit/Views/Tenants/Index.cshtml", invalidVm);
        }

        var result = await _leaseAssignmentService.UpdateFromUnitAsync(
            access.context!,
            new LeaseUpdateRequest
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
            return View("~/Areas/Unit/Views/Tenants/Index.cshtml", invalidVm);
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

        var result = await _leaseAssignmentService.DeleteFromUnitAsync(
            access.context!,
            new LeaseDeleteRequest
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

    private async Task<(IActionResult? response, UnitDashboardContext? context)> ResolveUnitContextAsync(
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

        var unitAccess = await _unitAccessService.ResolveUnitDashboardContextAsync(
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

    private async Task<TenantsPageViewModel> BuildPageViewModelAsync(
        UnitDashboardContext context,
        CancellationToken cancellationToken,
        AddUnitLeaseViewModel? addOverride = null,
        EditUnitLeaseViewModel? editOverride = null,
        Guid? requestedEditLeaseId = null)
    {
        var leaseList = await _leaseAssignmentService.ListForUnitAsync(context, cancellationToken);
        var roleOptions = await _leaseLookupService.ListLeaseRolesAsync(cancellationToken);
        var residentSearchTerm = addOverride?.ResidentSearchTerm;
        var residentResults = string.IsNullOrWhiteSpace(residentSearchTerm)
            ? Array.Empty<LeaseResidentSearchItem>()
            : (await _leaseLookupService.SearchResidentsAsync(context, residentSearchTerm, cancellationToken)).Residents;

        var unitLayout = new LayoutViewModel
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

        return new TenantsPageViewModel
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
        LayoutViewModel layout)
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
            Unit = layout
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

    private static void ApplyCreateErrors(LeaseCommandResult result, ModelStateDictionary modelState)
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

    private static void ApplyEditErrors(LeaseCommandResult result, ModelStateDictionary modelState)
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
