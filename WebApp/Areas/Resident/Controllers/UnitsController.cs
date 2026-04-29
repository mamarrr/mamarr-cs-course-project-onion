using System.Security.Claims;
using App.BLL.Contracts.Residents.Services;
using App.BLL.LeaseAssignments;
using App.BLL.ResidentWorkspace.Access;
using App.BLL.ResidentWorkspace.Residents;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Resident;

namespace WebApp.Areas.Resident.Controllers;

[Area("Resident")]
[Authorize]
[Route("m/{companySlug}/r/{residentIdCode}/units")]
public class UnitsController : Controller
{
    private const string SuccessTempDataKey = "ResidentUnitsSuccess";
    private const string ErrorTempDataKey = "ResidentUnitsError";

    private readonly IResidentAccessService _residentAccessService;
    private readonly ILeaseAssignmentService _leaseAssignmentService;
    private readonly ILeaseLookupService _leaseLookupService;
    private readonly IAppChromeBuilder _appChromeBuilder;

    public UnitsController(
        IResidentAccessService residentAccessService,
        ILeaseAssignmentService leaseAssignmentService,
        ILeaseLookupService leaseLookupService,
        IAppChromeBuilder appChromeBuilder)
    {
        _residentAccessService = residentAccessService;
        _leaseAssignmentService = leaseAssignmentService;
        _leaseLookupService = leaseLookupService;
        _appChromeBuilder = appChromeBuilder;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string companySlug,
        string residentIdCode,
        [FromQuery] UnitsPageViewModel? query,
        CancellationToken cancellationToken)
    {
        var access = await ResolveResidentContextAsync(companySlug, residentIdCode, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var vm = await BuildPageViewModelAsync(
            access.context!,
            cancellationToken,
            addOverride: query?.AddLease,
            requestedEditLeaseId: query?.ActiveEditLeaseId);
        return View("~/Areas/Resident/Views/Units/Index.cshtml", vm);
    }

    [HttpGet("property-search")]
    public async Task<IActionResult> SearchProperties(
        string companySlug,
        string residentIdCode,
        [FromQuery] string? searchTerm,
        CancellationToken cancellationToken)
    {
        var access = await ResolveResidentContextAsync(companySlug, residentIdCode, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var result = await _leaseLookupService.SearchPropertiesAsync(access.context!, searchTerm, cancellationToken);

        return Json(result.Properties.Select(x => new ResidentLeasePropertySearchResultViewModel
        {
            PropertyId = x.PropertyId,
            PropertyName = x.PropertyName,
            CustomerName = x.CustomerName,
            AddressLine = x.AddressLine,
            City = x.City,
            PostalCode = x.PostalCode
        }));
    }

    [HttpGet("properties/{propertyId:guid}/units")]
    public async Task<IActionResult> ListUnitsForProperty(
        string companySlug,
        string residentIdCode,
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var access = await ResolveResidentContextAsync(companySlug, residentIdCode, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var result = await _leaseLookupService.ListUnitsForPropertyAsync(access.context!, propertyId, cancellationToken);
        if (!result.Success)
        {
            return NotFound();
        }

        return Json(result.Units.Select(x => new ResidentLeaseUnitOptionViewModel
        {
            UnitId = x.UnitId,
            UnitNr = x.UnitNr,
            FloorNr = x.FloorNr,
            IsActive = x.IsActive
        }));
    }

    [HttpPost("leases/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        string companySlug,
        string residentIdCode,
        UnitsPageViewModel vm,
        CancellationToken cancellationToken)
    {
        var access = await ResolveResidentContextAsync(companySlug, residentIdCode, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        foreach (var key in ModelState.Keys
                     .Where(k => k.StartsWith($"{nameof(UnitsPageViewModel.EditLease)}.", StringComparison.Ordinal))
                     .ToList())
        {
            ModelState.Remove(key);
        }

        if (!TryValidateModel(vm.AddLease, nameof(UnitsPageViewModel.AddLease)))
        {
            var invalidVm = await BuildPageViewModelAsync(access.context!, cancellationToken, addOverride: vm.AddLease);
            return View("~/Areas/Resident/Views/Units/Index.cshtml", invalidVm);
        }

        var result = await _leaseAssignmentService.CreateFromResidentAsync(
            access.context!,
            new LeaseCreateRequest
            {
                ResidentId = access.context!.ResidentId,
                UnitId = vm.AddLease.UnitId!.Value,
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
            return View("~/Areas/Resident/Views/Units/Index.cshtml", invalidVm);
        }

        TempData[SuccessTempDataKey] = UiText.LeaseAddedSuccessfully;
        return RedirectToAction(nameof(Index), new { companySlug, residentIdCode });
    }

    [HttpPost("leases/{leaseId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string companySlug,
        string residentIdCode,
        Guid leaseId,
        UnitsPageViewModel vm,
        CancellationToken cancellationToken)
    {
        var access = await ResolveResidentContextAsync(companySlug, residentIdCode, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var editVm = vm.EditLease;
        editVm.LeaseId = leaseId;

        foreach (var key in ModelState.Keys
                     .Where(k => k.StartsWith($"{nameof(UnitsPageViewModel.AddLease)}.", StringComparison.Ordinal))
                     .ToList())
        {
            ModelState.Remove(key);
        }

        if (!TryValidateModel(editVm, nameof(UnitsPageViewModel.EditLease)))
        {
            var invalidVm = await BuildPageViewModelAsync(access.context!, cancellationToken, editOverride: editVm);
            invalidVm.ActiveEditLeaseId = leaseId;
            return View("~/Areas/Resident/Views/Units/Index.cshtml", invalidVm);
        }

        var result = await _leaseAssignmentService.UpdateFromResidentAsync(
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
            var invalidVm = await BuildPageViewModelAsync(access.context!, cancellationToken, editOverride: editVm);
            invalidVm.ActiveEditLeaseId = leaseId;
            return View("~/Areas/Resident/Views/Units/Index.cshtml", invalidVm);
        }

        TempData[SuccessTempDataKey] = UiText.LeaseUpdatedSuccessfully;
        return RedirectToAction(nameof(Index), new { companySlug, residentIdCode });
    }

    [HttpPost("leases/{leaseId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string companySlug,
        string residentIdCode,
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        var access = await ResolveResidentContextAsync(companySlug, residentIdCode, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var result = await _leaseAssignmentService.DeleteFromResidentAsync(
            access.context!,
            new LeaseDeleteRequest
            {
                LeaseId = leaseId
            },
            cancellationToken);

        if (!result.Success)
        {
            TempData[ErrorTempDataKey] = result.ErrorMessage ?? UiText.UnableToDeleteLease;
            return RedirectToAction(nameof(Index), new { companySlug, residentIdCode });
        }

        TempData[SuccessTempDataKey] = UiText.LeaseDeletedSuccessfully;
        return RedirectToAction(nameof(Index), new { companySlug, residentIdCode });
    }

    private async Task<(IActionResult? response, ResidentDashboardContext? context)> ResolveResidentContextAsync(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (Challenge(), null);
        }

        var access = await _residentAccessService.ResolveDashboardAccessAsync(
            appUserId.Value,
            companySlug,
            residentIdCode,
            cancellationToken);

        if (access.CompanyNotFound || access.ResidentNotFound)
        {
            return (NotFound(), null);
        }

        if (access.IsForbidden || access.Context == null)
        {
            return (Forbid(), null);
        }

        return (null, access.Context);
    }

    private async Task<UnitsPageViewModel> BuildPageViewModelAsync(
        ResidentDashboardContext context,
        CancellationToken cancellationToken,
        AddResidentLeaseViewModel? addOverride = null,
        EditResidentLeaseViewModel? editOverride = null,
        Guid? requestedEditLeaseId = null)
    {
        var leaseList = await _leaseAssignmentService.ListForResidentAsync(context, cancellationToken);
        var roleOptions = await _leaseLookupService.ListLeaseRolesAsync(cancellationToken);
        var propertySearchTerm = addOverride?.PropertySearchTerm;

        var propertyResults = string.IsNullOrWhiteSpace(propertySearchTerm)
            ? Array.Empty<LeasePropertySearchItem>()
            : (await _leaseLookupService.SearchPropertiesAsync(context, propertySearchTerm, cancellationToken)).Properties;

        var selectedPropertyId = addOverride?.PropertyId;
        IReadOnlyList<LeaseUnitOption> unitOptions = Array.Empty<LeaseUnitOption>();
        if (selectedPropertyId.HasValue)
        {
            var unitsResult = await _leaseLookupService.ListUnitsForPropertyAsync(context, selectedPropertyId.Value, cancellationToken);
            if (unitsResult.Success)
            {
                unitOptions = unitsResult.Units;
            }
        }

        var residentDisplayName = string.IsNullOrWhiteSpace(context.FullName) ? context.ResidentIdCode : context.FullName;
        var residentSupportingText = string.IsNullOrWhiteSpace(context.FullName) ? null : context.ResidentIdCode;

        var leases = leaseList.Leases.Select(x => new ResidentLeaseListItemViewModel
        {
            LeaseId = x.LeaseId,
            PropertyId = x.PropertyId,
            PropertyName = x.PropertyName,
            UnitId = x.UnitId,
            UnitNr = x.UnitNr,
            LeaseRoleId = x.LeaseRoleId,
            LeaseRoleLabel = x.LeaseRoleLabel,
            StartDate = x.StartDate.ToDateTime(TimeOnly.MinValue),
            EndDate = x.EndDate?.ToDateTime(TimeOnly.MinValue),
            IsActive = x.IsActive,
            Notes = x.Notes
        }).ToList();

        var activeEditLeaseId = requestedEditLeaseId;
        var editLease = editOverride ?? new EditResidentLeaseViewModel();
        if (activeEditLeaseId.HasValue && editOverride == null)
        {
            var selectedLease = leases.FirstOrDefault(x => x.LeaseId == activeEditLeaseId.Value);
            if (selectedLease != null)
            {
                editLease = new EditResidentLeaseViewModel
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

        return new UnitsPageViewModel
        {
            AppChrome = await BuildAppChromeAsync(context, UiText.Units, cancellationToken),
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentIdCode = context.ResidentIdCode,
            ResidentDisplayName = residentDisplayName,
            ResidentSupportingText = residentSupportingText,
            SuccessMessage = TempData[SuccessTempDataKey] as string,
            ErrorMessage = TempData[ErrorTempDataKey] as string,
            ActiveEditLeaseId = activeEditLeaseId,
            Leases = leases,
            LeaseRoleOptions = roleOptions.Roles.Select(x => new ResidentLeaseRoleOptionViewModel
            {
                LeaseRoleId = x.LeaseRoleId,
                Label = x.Label
            }).ToList(),
            PropertySearchResults = propertyResults.Select(x => new ResidentLeasePropertySearchResultViewModel
            {
                PropertyId = x.PropertyId,
                PropertyName = x.PropertyName,
                CustomerName = x.CustomerName,
                AddressLine = x.AddressLine,
                City = x.City,
                PostalCode = x.PostalCode
            }).ToList(),
            UnitOptions = unitOptions.Select(x => new ResidentLeaseUnitOptionViewModel
            {
                UnitId = x.UnitId,
                UnitNr = x.UnitNr,
                FloorNr = x.FloorNr,
                IsActive = x.IsActive
            }).ToList(),
            AddLease = addOverride ?? new AddResidentLeaseViewModel(),
            EditLease = editLease
        };
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        ResidentDashboardContext context,
        string title,
        CancellationToken cancellationToken)
    {
        var residentDisplayName = string.IsNullOrWhiteSpace(context.FullName)
            ? context.ResidentIdCode
            : context.FullName;

        return _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = title,
                ActiveSection = Sections.Units,
                ManagementCompanySlug = context.CompanySlug,
                ManagementCompanyName = context.CompanyName,
                ResidentIdCode = context.ResidentIdCode,
                ResidentDisplayName = residentDisplayName,
                ResidentSupportingText = string.IsNullOrWhiteSpace(context.FullName) ? null : context.ResidentIdCode,
                CurrentLevel = WorkspaceLevel.Resident
            },
            cancellationToken);
    }

    private Guid? GetAppUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }

    private static void ApplyCreateErrors(LeaseCommandResult result, ModelStateDictionary modelState)
    {
        if (result.PropertyNotFound)
        {
            modelState.AddModelError("AddLease.PropertyId", UiText.UnableToLoadProperty);
            return;
        }

        if (result.UnitNotFound)
        {
            modelState.AddModelError("AddLease.UnitId", result.ErrorMessage ?? UiText.UnableToLoadUnit);
            return;
        }

        if (result.InvalidLeaseRole)
        {
            modelState.AddModelError("AddLease.LeaseRoleId", result.ErrorMessage ?? UiText.InvalidLeaseRole);
            return;
        }

        if (result.InvalidStartDate)
        {
            modelState.AddModelError("AddLease.StartDate", result.ErrorMessage ?? UiText.RequiredField);
            return;
        }

        if (result.InvalidEndDate)
        {
            modelState.AddModelError("AddLease.EndDate", result.ErrorMessage ?? UiText.InvalidEndDate);
            return;
        }

        if (result.DuplicateActiveLease)
        {
            modelState.AddModelError(string.Empty, result.ErrorMessage ?? UiText.DuplicateActiveLeaseForResident);
            return;
        }

        modelState.AddModelError(string.Empty, result.ErrorMessage ?? UiText.UnableToAddLease);
    }

    private static void ApplyEditErrors(LeaseCommandResult result, ModelStateDictionary modelState)
    {
        if (result.LeaseNotFound)
        {
            modelState.AddModelError(string.Empty, result.ErrorMessage ?? UiText.UnableToLoadLease);
            return;
        }

        if (result.InvalidLeaseRole)
        {
            modelState.AddModelError("EditLease.LeaseRoleId", result.ErrorMessage ?? UiText.InvalidLeaseRole);
            return;
        }

        if (result.InvalidStartDate)
        {
            modelState.AddModelError("EditLease.StartDate", result.ErrorMessage ?? UiText.RequiredField);
            return;
        }

        if (result.InvalidEndDate)
        {
            modelState.AddModelError("EditLease.EndDate", result.ErrorMessage ?? UiText.InvalidEndDate);
            return;
        }

        if (result.DuplicateActiveLease)
        {
            modelState.AddModelError(string.Empty, result.ErrorMessage ?? UiText.DuplicateActiveLeaseForResident);
            return;
        }

        modelState.AddModelError(string.Empty, result.ErrorMessage ?? UiText.UnableToUpdateLease);
    }

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
