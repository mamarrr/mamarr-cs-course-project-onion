using System.Security.Claims;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Leases;
using App.BLL.Contracts.Leases.Commands;
using App.BLL.Contracts.Leases.Models;
using App.BLL.Contracts.Leases.Queries;
using App.BLL.Contracts.Residents;
using App.BLL.Contracts.Residents.Models;
using App.BLL.Contracts.Residents.Queries;
using App.BLL.Mappers.Leases;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebApp.Mappers.Mvc.Leases;
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
    private readonly LeaseViewModelMapper _leaseMapper;

    public UnitsController(
        IResidentAccessService residentAccessService,
        ILeaseAssignmentService leaseAssignmentService,
        ILeaseLookupService leaseLookupService,
        IAppChromeBuilder appChromeBuilder,
        LeaseViewModelMapper leaseMapper)
    {
        _residentAccessService = residentAccessService;
        _leaseAssignmentService = leaseAssignmentService;
        _leaseLookupService = leaseLookupService;
        _appChromeBuilder = appChromeBuilder;
        _leaseMapper = leaseMapper;
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

        var result = await _leaseLookupService.SearchPropertiesAsync(
            ToSearchPropertiesQuery(access.context!, searchTerm),
            cancellationToken);

        return Json(result.Value.Properties.Select(x => new ResidentLeasePropertySearchResultViewModel
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

        var result = await _leaseLookupService.ListUnitsForPropertyAsync(
            ToUnitsForPropertyQuery(access.context!, propertyId),
            cancellationToken);
        if (result.IsFailed)
        {
            return NotFound();
        }

        return Json(result.Value.Units.Select(x => new ResidentLeaseUnitOptionViewModel
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
            ToCreateCommand(access.context!, vm.AddLease),
            cancellationToken);

        if (result.IsFailed)
        {
            ApplyCreateErrors(result.Errors, ModelState);
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
            ToUpdateCommand(access.context!, leaseId, editVm),
            cancellationToken);

        if (result.IsFailed)
        {
            ApplyEditErrors(result.Errors, ModelState);
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
            ToDeleteCommand(access.context!, leaseId),
            cancellationToken);

        if (result.IsFailed)
        {
            TempData[ErrorTempDataKey] = result.Errors.FirstOrDefault()?.Message ?? UiText.UnableToDeleteLease;
            return RedirectToAction(nameof(Index), new { companySlug, residentIdCode });
        }

        TempData[SuccessTempDataKey] = UiText.LeaseDeletedSuccessfully;
        return RedirectToAction(nameof(Index), new { companySlug, residentIdCode });
    }

    private async Task<(IActionResult? response, ResidentWorkspaceModel? context)> ResolveResidentContextAsync(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (Challenge(), null);
        }

        var access = await _residentAccessService.ResolveResidentWorkspaceAsync(
            new GetResidentProfileQuery
            {
                UserId = appUserId.Value,
                CompanySlug = companySlug,
                ResidentIdCode = residentIdCode
            },
            cancellationToken);

        if (access.Errors.OfType<NotFoundError>().Any())
        {
            return (NotFound(), null);
        }

        if (access.Errors.OfType<ForbiddenError>().Any())
        {
            return (Forbid(), null);
        }

        return access.IsFailed ? (BadRequest(), null) : (null, access.Value);
    }

    private async Task<UnitsPageViewModel> BuildPageViewModelAsync(
        ResidentWorkspaceModel context,
        CancellationToken cancellationToken,
        AddResidentLeaseViewModel? addOverride = null,
        EditResidentLeaseViewModel? editOverride = null,
        Guid? requestedEditLeaseId = null)
    {
        var leaseList = await _leaseAssignmentService.ListForResidentAsync(
            LeaseBllMapper.ToResidentLeasesQuery(context),
            cancellationToken);
        var roleOptions = await _leaseLookupService.ListLeaseRolesAsync(cancellationToken);
        var propertySearchTerm = addOverride?.PropertySearchTerm;

        var propertyResults = string.IsNullOrWhiteSpace(propertySearchTerm)
            ? Array.Empty<LeasePropertySearchItemModel>()
            : (await _leaseLookupService.SearchPropertiesAsync(ToSearchPropertiesQuery(context, propertySearchTerm), cancellationToken)).Value.Properties;

        var selectedPropertyId = addOverride?.PropertyId;
        IReadOnlyList<LeaseUnitOptionModel> unitOptions = Array.Empty<LeaseUnitOptionModel>();
        if (selectedPropertyId.HasValue)
        {
            var unitsResult = await _leaseLookupService.ListUnitsForPropertyAsync(
                ToUnitsForPropertyQuery(context, selectedPropertyId.Value),
                cancellationToken);
            if (unitsResult.IsSuccess)
            {
                unitOptions = unitsResult.Value.Units;
            }
        }

        var residentDisplayName = string.IsNullOrWhiteSpace(context.FullName) ? context.ResidentIdCode : context.FullName;
        var residentSupportingText = string.IsNullOrWhiteSpace(context.FullName) ? null : context.ResidentIdCode;
        var leases = leaseList.Value.Leases.Select(_leaseMapper.ToResidentLeaseViewModel).ToList();

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
            LeaseRoleOptions = roleOptions.Value.Roles.Select(x => new ResidentLeaseRoleOptionViewModel
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
        ResidentWorkspaceModel context,
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

    private static void ApplyCreateErrors(IReadOnlyList<IError> errors, ModelStateDictionary modelState)
    {
        var failure = errors.OfType<ValidationAppError>().FirstOrDefault()?.Failures.FirstOrDefault();
        if (failure?.PropertyName == "UnitId")
        {
            modelState.AddModelError("AddLease.UnitId", failure.ErrorMessage);
            return;
        }

        ApplySharedErrors(errors, modelState, "AddLease", UiText.UnableToAddLease);
    }

    private static void ApplyEditErrors(IReadOnlyList<IError> errors, ModelStateDictionary modelState)
    {
        if (errors.OfType<NotFoundError>().Any())
        {
            modelState.AddModelError(string.Empty, errors.First().Message);
            return;
        }

        ApplySharedErrors(errors, modelState, "EditLease", UiText.UnableToUpdateLease);
    }

    private static void ApplySharedErrors(
        IReadOnlyList<IError> errors,
        ModelStateDictionary modelState,
        string prefix,
        string fallback)
    {
        var failure = errors.OfType<ValidationAppError>().FirstOrDefault()?.Failures.FirstOrDefault();
        if (failure?.PropertyName == "LeaseRoleId")
        {
            modelState.AddModelError($"{prefix}.LeaseRoleId", failure.ErrorMessage);
            return;
        }

        if (failure?.PropertyName == "StartDate")
        {
            modelState.AddModelError($"{prefix}.StartDate", failure.ErrorMessage);
            return;
        }

        if (failure?.PropertyName == "EndDate")
        {
            modelState.AddModelError($"{prefix}.EndDate", failure.ErrorMessage);
            return;
        }

        var conflict = errors.OfType<ConflictError>().FirstOrDefault();
        if (conflict is not null)
        {
            modelState.AddModelError(string.Empty, conflict.Message);
            return;
        }

        modelState.AddModelError(string.Empty, errors.FirstOrDefault()?.Message ?? fallback);
    }

    private static CreateLeaseFromResidentCommand ToCreateCommand(
        ResidentWorkspaceModel context,
        AddResidentLeaseViewModel vm)
    {
        return new CreateLeaseFromResidentCommand
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentId = context.ResidentId,
            ResidentIdCode = context.ResidentIdCode,
            FullName = context.FullName,
            UnitId = vm.UnitId!.Value,
            LeaseRoleId = vm.LeaseRoleId!.Value,
            StartDate = DateOnly.FromDateTime(vm.StartDate),
            EndDate = vm.EndDate.HasValue ? DateOnly.FromDateTime(vm.EndDate.Value) : null,
            IsActive = vm.IsActive,
            Notes = vm.Notes
        };
    }

    private static UpdateLeaseFromResidentCommand ToUpdateCommand(
        ResidentWorkspaceModel context,
        Guid leaseId,
        EditResidentLeaseViewModel vm)
    {
        return new UpdateLeaseFromResidentCommand
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentId = context.ResidentId,
            ResidentIdCode = context.ResidentIdCode,
            FullName = context.FullName,
            LeaseId = leaseId,
            LeaseRoleId = vm.LeaseRoleId!.Value,
            StartDate = DateOnly.FromDateTime(vm.StartDate),
            EndDate = vm.EndDate.HasValue ? DateOnly.FromDateTime(vm.EndDate.Value) : null,
            IsActive = vm.IsActive,
            Notes = vm.Notes
        };
    }

    private static DeleteLeaseFromResidentCommand ToDeleteCommand(
        ResidentWorkspaceModel context,
        Guid leaseId)
    {
        return new DeleteLeaseFromResidentCommand
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentId = context.ResidentId,
            ResidentIdCode = context.ResidentIdCode,
            FullName = context.FullName,
            LeaseId = leaseId
        };
    }

    private static SearchLeasePropertiesQuery ToSearchPropertiesQuery(
        ResidentWorkspaceModel context,
        string? searchTerm)
    {
        return new SearchLeasePropertiesQuery
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentId = context.ResidentId,
            ResidentIdCode = context.ResidentIdCode,
            FullName = context.FullName,
            SearchTerm = searchTerm
        };
    }

    private static GetLeaseUnitsForPropertyQuery ToUnitsForPropertyQuery(
        ResidentWorkspaceModel context,
        Guid propertyId)
    {
        return new GetLeaseUnitsForPropertyQuery
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentId = context.ResidentId,
            ResidentIdCode = context.ResidentIdCode,
            FullName = context.FullName,
            PropertyId = propertyId
        };
    }
}
