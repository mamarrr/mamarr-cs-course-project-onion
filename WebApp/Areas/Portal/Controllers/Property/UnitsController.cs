using App.BLL.Contracts;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Units.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers.Mvc.Units;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Property;

namespace WebApp.Areas.Portal.Controllers.Property;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units")]
public class UnitsController : Controller
{
    private readonly IAppBLL _bll;
    private readonly UnitMvcMapper _unitMapper;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public UnitsController(
        IAppBLL bll,
        UnitMvcMapper unitMapper,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver)
    {
        _bll = bll;
        _unitMapper = unitMapper;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
    }

    [HttpGet("")]
    public async Task<IActionResult> Units(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var result = await _bll.UnitWorkspaces.GetPropertyUnitsAsync(
            _unitMapper.ToPropertyUnitsQuery(companySlug, customerSlug, propertySlug, appUserId.Value),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        var vm = await BuildUnitsPageViewModelAsync(result.Value, cancellationToken);
        return View("~/Areas/Portal/Views/Property/Units/Index.cshtml", vm);
    }

    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUnit(
        string companySlug,
        string customerSlug,
        string propertySlug,
        UnitsPageViewModel vm,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var listResult = await _bll.UnitWorkspaces.GetPropertyUnitsAsync(
            _unitMapper.ToPropertyUnitsQuery(companySlug, customerSlug, propertySlug, appUserId.Value),
            cancellationToken);
        if (listResult.IsFailed)
        {
            return ToMvcErrorResult(listResult.Errors);
        }

        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildUnitsPageViewModelAsync(listResult.Value, cancellationToken, vm.AddUnit);
            return View("~/Areas/Portal/Views/Property/Units/Index.cshtml", invalidVm);
        }

        var createResult = await _bll.UnitWorkspaces.CreateAsync(
            _unitMapper.ToCreateCommand(companySlug, customerSlug, propertySlug, vm.AddUnit, appUserId.Value),
            cancellationToken);

        if (createResult.IsFailed)
        {
            ApplyCreateErrors(createResult.Errors);
            var invalidVm = await BuildUnitsPageViewModelAsync(listResult.Value, cancellationToken, vm.AddUnit);
            return View("~/Areas/Portal/Views/Property/Units/Index.cshtml", invalidVm);
        }

        TempData["PropertyUnitsSuccess"] = T("UnitAddedSuccessfully", "Unit added successfully.");
        return RedirectToAction(nameof(Units), new { companySlug, customerSlug, propertySlug });
    }

    private async Task<UnitsPageViewModel> BuildUnitsPageViewModelAsync(
        PropertyUnitsModel model,
        CancellationToken cancellationToken,
        AddUnitViewModel? addUnitOverride = null)
    {
        return new UnitsPageViewModel
        {
            AppChrome = await BuildAppChromeAsync(model, UiText.Units, cancellationToken),
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            CustomerSlug = model.CustomerSlug,
            CustomerName = model.CustomerName,
            PropertySlug = model.PropertySlug,
            PropertyName = model.PropertyName,
            Units = model.Units.Select(x => new PropertyUnitListItemViewModel
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
        PropertyUnitsModel model,
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
                ManagementCompanySlug = model.CompanySlug,
                ManagementCompanyName = model.CompanyName,
                CustomerSlug = model.CustomerSlug,
                CustomerName = model.CustomerName,
                PropertySlug = model.PropertySlug,
                PropertyName = model.PropertyName,
                CurrentLevel = WorkspaceLevel.Property
            },
            cancellationToken);
    }

    private void ApplyCreateErrors(IReadOnlyList<IError> errors)
    {
        var validation = errors.OfType<ValidationAppError>().FirstOrDefault();
        if (validation is null)
        {
            ModelState.AddModelError(
                string.Empty,
                errors.FirstOrDefault()?.Message ?? T("ErrorOccurred", "An error occurred while processing your request."));
            return;
        }

        foreach (var failure in validation.Failures)
        {
            var key = failure.PropertyName switch
            {
                nameof(App.BLL.Contracts.Units.Commands.CreateUnitCommand.UnitNr) =>
                    nameof(UnitsPageViewModel.AddUnit) + "." + nameof(AddUnitViewModel.UnitNr),
                nameof(App.BLL.Contracts.Units.Commands.CreateUnitCommand.FloorNr) =>
                    nameof(UnitsPageViewModel.AddUnit) + "." + nameof(AddUnitViewModel.FloorNr),
                nameof(App.BLL.Contracts.Units.Commands.CreateUnitCommand.SizeM2) =>
                    nameof(UnitsPageViewModel.AddUnit) + "." + nameof(AddUnitViewModel.SizeM2),
                _ => string.Empty
            };

            ModelState.AddModelError(key, failure.ErrorMessage);
        }
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
