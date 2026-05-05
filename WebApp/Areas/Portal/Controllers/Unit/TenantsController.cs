using App.BLL.Contracts;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Leases;
using App.BLL.Contracts.Leases.Commands;
using App.BLL.Contracts.Leases.Models;
using App.BLL.Contracts.Leases.Queries;
using App.BLL.Contracts.Units.Models;
using App.BLL.Mappers.Leases;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebApp.Mappers.Mvc.Leases;
using WebApp.Mappers.Mvc.Units;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Unit;

namespace WebApp.Areas.Portal.Controllers.Unit;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/tenants")]
public class TenantsController : Controller
{
    private const string SuccessTempDataKey = "UnitTenantsSuccess";
    private const string ErrorTempDataKey = "UnitTenantsError";

    private readonly IAppBLL _bll;
    private readonly UnitMvcMapper _unitMapper;
    private readonly LeaseViewModelMapper _leaseMapper;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public TenantsController(
        IAppBLL bll,
        UnitMvcMapper unitMapper,
        LeaseViewModelMapper leaseMapper,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver)
    {
        _bll = bll;
        _unitMapper = unitMapper;
        _leaseMapper = leaseMapper;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
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
        return View("~/Areas/Portal/Views/Unit/Tenants/Index.cshtml", vm);
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

        var result = await _bll.LeaseLookups.SearchResidentsAsync(
            ToSearchResidentsQuery(access.context!, searchTerm),
            cancellationToken);

        return Json(result.Value.Residents.Select(x => new UnitLeaseResidentSearchResultViewModel
        {
            ResidentId = x.ResidentId,
            FullName = x.FullName,
            IdCode = x.IdCode,
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
            return View("~/Areas/Portal/Views/Unit/Tenants/Index.cshtml", invalidVm);
        }

        var result = await _bll.LeaseAssignments.CreateFromUnitAsync(
            ToCreateCommand(access.context!, vm.AddLease),
            cancellationToken);

        if (result.IsFailed)
        {
            ApplyCreateErrors(result.Errors, ModelState);
            var invalidVm = await BuildPageViewModelAsync(access.context!, cancellationToken, addOverride: vm.AddLease);
            return View("~/Areas/Portal/Views/Unit/Tenants/Index.cshtml", invalidVm);
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
            return View("~/Areas/Portal/Views/Unit/Tenants/Index.cshtml", invalidVm);
        }

        var result = await _bll.LeaseAssignments.UpdateFromUnitAsync(
            ToUpdateCommand(access.context!, leaseId, editVm),
            cancellationToken);

        if (result.IsFailed)
        {
            ApplyEditErrors(result.Errors, ModelState);
            var invalidVm = await BuildPageViewModelAsync(access.context!, cancellationToken, editOverride: editVm, requestedEditLeaseId: leaseId);
            invalidVm.ActiveEditLeaseId = leaseId;
            return View("~/Areas/Portal/Views/Unit/Tenants/Index.cshtml", invalidVm);
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

        var result = await _bll.LeaseAssignments.DeleteFromUnitAsync(
            ToDeleteCommand(access.context!, leaseId),
            cancellationToken);

        if (result.IsFailed)
        {
            TempData[ErrorTempDataKey] = result.Errors.FirstOrDefault()?.Message ?? T("UnableToDeleteLease", "Unable to delete lease.");
            return RedirectToAction(nameof(Index), new { companySlug, customerSlug, propertySlug, unitSlug });
        }

        TempData[SuccessTempDataKey] = T("LeaseDeletedSuccessfully", "Lease deleted successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, customerSlug, propertySlug, unitSlug });
    }

    private async Task<(IActionResult? response, UnitWorkspaceModel? context)> ResolveUnitContextAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return (Challenge(), null);
        }

        var unitAccess = await _bll.UnitAccess.ResolveUnitWorkspaceAsync(
            _unitMapper.ToDashboardQuery(companySlug, customerSlug, propertySlug, unitSlug, appUserId.Value),
            cancellationToken);
        if (unitAccess.IsFailed)
        {
            return (ToMvcErrorResult(unitAccess.Errors), null);
        }

        return (null, unitAccess.Value);
    }

    private async Task<TenantsPageViewModel> BuildPageViewModelAsync(
        UnitWorkspaceModel context,
        CancellationToken cancellationToken,
        AddUnitLeaseViewModel? addOverride = null,
        EditUnitLeaseViewModel? editOverride = null,
        Guid? requestedEditLeaseId = null)
    {
        var leaseList = await _bll.LeaseAssignments.ListForUnitAsync(
            LeaseBllMapper.ToUnitLeasesQuery(context),
            cancellationToken);
        var roleOptions = await _bll.LeaseLookups.ListLeaseRolesAsync(cancellationToken);
        var residentSearchTerm = addOverride?.ResidentSearchTerm;
        var residentResults = string.IsNullOrWhiteSpace(residentSearchTerm)
            ? Array.Empty<LeaseResidentSearchItemModel>()
            : (await _bll.LeaseLookups.SearchResidentsAsync(ToSearchResidentsQuery(context, residentSearchTerm), cancellationToken)).Value.Residents;

        var leases = leaseList.Value.Leases.Select(_leaseMapper.ToUnitLeaseViewModel).ToList();

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
                    Notes = selectedLease.Notes
                };
            }
        }

        return new TenantsPageViewModel
        {
            AppChrome = await BuildAppChromeAsync(context, T("Tenants", "Tenants"), cancellationToken),
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
            LeaseRoleOptions = roleOptions.Value.Roles.Select(x => new UnitLeaseRoleOptionViewModel
            {
                LeaseRoleId = x.LeaseRoleId,
                Label = x.Label
            }).ToList(),
            ResidentSearchResults = residentResults.Select(x => new UnitLeaseResidentSearchResultViewModel
            {
                ResidentId = x.ResidentId,
                FullName = x.FullName,
                IdCode = x.IdCode,
            }).ToList(),
            AddLease = addOverride ?? new AddUnitLeaseViewModel(),
            EditLease = editLease
        };
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        UnitWorkspaceModel context,
        string title,
        CancellationToken cancellationToken)
    {
        return _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = title,
                ActiveSection = Sections.Tenants,
                ManagementCompanySlug = context.CompanySlug,
                ManagementCompanyName = context.CompanyName,
                CustomerSlug = context.CustomerSlug,
                CustomerName = context.CustomerName,
                PropertySlug = context.PropertySlug,
                PropertyName = context.PropertyName,
                UnitSlug = context.UnitSlug,
                UnitName = context.UnitNr,
                CurrentLevel = WorkspaceLevel.Unit
            },
            cancellationToken);
    }

    private static void ApplyCreateErrors(IReadOnlyList<IError> errors, ModelStateDictionary modelState)
    {
        var failure = errors.OfType<ValidationAppError>().FirstOrDefault()?.Failures.FirstOrDefault();
        if (failure?.PropertyName == "ResidentId")
        {
            modelState.AddModelError("AddLease.ResidentId", failure.ErrorMessage);
            return;
        }

        ApplySharedErrors(errors, modelState, "AddLease", T("UnableToAddLease", "Unable to add lease."));
    }

    private static void ApplyEditErrors(IReadOnlyList<IError> errors, ModelStateDictionary modelState)
    {
        if (errors.OfType<NotFoundError>().Any())
        {
            modelState.AddModelError(string.Empty, errors.First().Message);
            return;
        }

        ApplySharedErrors(errors, modelState, "EditLease", T("UnableToUpdateLease", "Unable to update lease."));
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

    private static CreateLeaseFromUnitCommand ToCreateCommand(
        UnitWorkspaceModel context,
        AddUnitLeaseViewModel vm)
    {
        return new CreateLeaseFromUnitCommand
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerId = context.CustomerId,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertyId = context.PropertyId,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitId = context.UnitId,
            UnitSlug = context.UnitSlug,
            UnitNr = context.UnitNr,
            ResidentId = vm.ResidentId!.Value,
            LeaseRoleId = vm.LeaseRoleId!.Value,
            StartDate = DateOnly.FromDateTime(vm.StartDate),
            EndDate = vm.EndDate.HasValue ? DateOnly.FromDateTime(vm.EndDate.Value) : null,
            Notes = vm.Notes
        };
    }

    private static UpdateLeaseFromUnitCommand ToUpdateCommand(
        UnitWorkspaceModel context,
        Guid leaseId,
        EditUnitLeaseViewModel vm)
    {
        return new UpdateLeaseFromUnitCommand
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerId = context.CustomerId,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertyId = context.PropertyId,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitId = context.UnitId,
            UnitSlug = context.UnitSlug,
            UnitNr = context.UnitNr,
            LeaseId = leaseId,
            LeaseRoleId = vm.LeaseRoleId!.Value,
            StartDate = DateOnly.FromDateTime(vm.StartDate),
            EndDate = vm.EndDate.HasValue ? DateOnly.FromDateTime(vm.EndDate.Value) : null,
            Notes = vm.Notes
        };
    }

    private static DeleteLeaseFromUnitCommand ToDeleteCommand(
        UnitWorkspaceModel context,
        Guid leaseId)
    {
        return new DeleteLeaseFromUnitCommand
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerId = context.CustomerId,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertyId = context.PropertyId,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitId = context.UnitId,
            UnitSlug = context.UnitSlug,
            UnitNr = context.UnitNr,
            LeaseId = leaseId
        };
    }

    private static SearchLeaseResidentsQuery ToSearchResidentsQuery(
        UnitWorkspaceModel context,
        string? searchTerm)
    {
        return new SearchLeaseResidentsQuery
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerId = context.CustomerId,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertyId = context.PropertyId,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitId = context.UnitId,
            UnitSlug = context.UnitSlug,
            UnitNr = context.UnitNr,
            SearchTerm = searchTerm
        };
    }

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }

    private Guid? GetAppUserId()
    {
        return _portalContextResolver.Resolve().AppUserId;
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
}
