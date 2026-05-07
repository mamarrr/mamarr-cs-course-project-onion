using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Vendors;
using App.BLL.DTO.Vendors.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.VendorCategories;

namespace WebApp.Areas.Portal.Controllers.Management;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/vendors/{vendorId:guid}/categories")]
public class VendorCategoriesController : Controller
{
    private readonly IAppBLL _bll;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public VendorCategoriesController(
        IAppBLL bll,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver)
    {
        _bll = bll;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string companySlug,
        Guid vendorId,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.Vendors.ListCategoryAssignmentsAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        return View(await ToIndexViewModelAsync(result.Value, new VendorCategoryFormViewModel(), cancellationToken));
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(
        string companySlug,
        Guid vendorId,
        VendorCategoryIndexViewModel vm,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var page = await BuildIndexPageAsync(route, vm.Form, cancellationToken);
            return page.response ?? View(nameof(Index), page.model);
        }

        var result = await _bll.Vendors.AssignCategoryAsync(
            route,
            new VendorTicketCategoryBllDto
            {
                TicketCategoryId = vm.Form.TicketCategoryId,
                Notes = vm.Form.Notes
            },
            cancellationToken);

        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            AddErrorsToModelState(result.Errors, "Form");
            var page = await BuildIndexPageAsync(route, vm.Form, cancellationToken);
            return page.response ?? View(nameof(Index), page.model);
        }

        TempData["ManagementVendorsSuccess"] = T("VendorCategoryAssignedSuccessfully", "Category assigned successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, vendorId });
    }

    [HttpGet("{ticketCategoryId:guid}/edit")]
    public async Task<IActionResult> Edit(
        string companySlug,
        Guid vendorId,
        Guid ticketCategoryId,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.Vendors.ListCategoryAssignmentsAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        var assignment = result.Value.Assignments.FirstOrDefault(item => item.TicketCategoryId == ticketCategoryId);
        if (assignment is null)
        {
            return NotFound();
        }

        return View(await ToEditViewModelAsync(result.Value, assignment, assignment.Notes, cancellationToken));
    }

    [HttpPost("{ticketCategoryId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string companySlug,
        Guid vendorId,
        Guid ticketCategoryId,
        VendorCategoryEditViewModel vm,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorCategoryRoute(companySlug, vendorId, ticketCategoryId);
        if (route is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var page = await BuildEditPageAsync(route, vm.Form.Notes, cancellationToken);
            return page.response ?? View(page.model);
        }

        var result = await _bll.Vendors.UpdateCategoryAssignmentAsync(
            route,
            new VendorTicketCategoryBllDto { Notes = vm.Form.Notes },
            cancellationToken);

        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            AddErrorsToModelState(result.Errors, "Form");
            var page = await BuildEditPageAsync(route, vm.Form.Notes, cancellationToken);
            return page.response ?? View(page.model);
        }

        TempData["ManagementVendorsSuccess"] = T("VendorCategoryUpdatedSuccessfully", "Category assignment updated successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, vendorId });
    }

    [HttpPost("{ticketCategoryId:guid}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(
        string companySlug,
        Guid vendorId,
        Guid ticketCategoryId,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorCategoryRoute(companySlug, vendorId, ticketCategoryId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.Vendors.RemoveCategoryAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            TempData["ManagementVendorsError"] = result.Errors.FirstOrDefault()?.Message
                                                 ?? T("UnableToRemoveVendorCategory", "Unable to remove category assignment.");
            return RedirectToAction(nameof(Index), new { companySlug, vendorId });
        }

        TempData["ManagementVendorsSuccess"] = T("VendorCategoryRemovedSuccessfully", "Category assignment removed successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, vendorId });
    }

    private async Task<(IActionResult? response, VendorCategoryIndexViewModel? model)> BuildIndexPageAsync(
        VendorRoute route,
        VendorCategoryFormViewModel form,
        CancellationToken cancellationToken)
    {
        var result = await _bll.Vendors.ListCategoryAssignmentsAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return (ToMvcErrorResult(result.Errors), null);
        }

        return (null, await ToIndexViewModelAsync(result.Value, form, cancellationToken));
    }

    private async Task<(IActionResult? response, VendorCategoryEditViewModel? model)> BuildEditPageAsync(
        VendorCategoryRoute route,
        string? notes,
        CancellationToken cancellationToken)
    {
        var result = await _bll.Vendors.ListCategoryAssignmentsAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return (ToMvcErrorResult(result.Errors), null);
        }

        var assignment = result.Value.Assignments.FirstOrDefault(item => item.TicketCategoryId == route.TicketCategoryId);
        if (assignment is null)
        {
            return (NotFound(), null);
        }

        return (null, await ToEditViewModelAsync(result.Value, assignment, notes, cancellationToken));
    }

    private async Task<VendorCategoryIndexViewModel> ToIndexViewModelAsync(
        VendorCategoryAssignmentListModel model,
        VendorCategoryFormViewModel form,
        CancellationToken cancellationToken)
    {
        return new VendorCategoryIndexViewModel
        {
            AppChrome = await BuildChromeAsync(
                model.CompanySlug,
                model.CompanyName,
                T("CategoryAssignments", "Category assignments"),
                cancellationToken),
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            VendorId = model.VendorId,
            VendorName = model.VendorName,
            Form = form,
            Assignments = model.Assignments.Select(ToAssignmentViewModel).ToList(),
            AvailableCategories = model.AvailableCategories
                .Select(category => new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = category.Label
                })
                .ToList()
        };
    }

    private async Task<VendorCategoryEditViewModel> ToEditViewModelAsync(
        VendorCategoryAssignmentListModel model,
        VendorCategoryAssignmentModel assignment,
        string? notes,
        CancellationToken cancellationToken)
    {
        return new VendorCategoryEditViewModel
        {
            AppChrome = await BuildChromeAsync(
                model.CompanySlug,
                model.CompanyName,
                T("EditCategoryAssignment", "Edit category assignment"),
                cancellationToken),
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            VendorId = model.VendorId,
            VendorName = model.VendorName,
            TicketCategoryId = assignment.TicketCategoryId,
            CategoryLabel = assignment.CategoryLabel,
            Form = new VendorCategoryNotesFormViewModel { Notes = notes }
        };
    }

    private async Task<AppChromeViewModel> BuildChromeAsync(
        string companySlug,
        string companyName,
        string pageTitle,
        CancellationToken cancellationToken)
    {
        return await _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = pageTitle,
                ActiveSection = Sections.Vendors,
                ManagementCompanySlug = companySlug,
                ManagementCompanyName = companyName,
                CurrentLevel = WorkspaceLevel.ManagementCompany
            },
            cancellationToken);
    }

    private static VendorCategoryAssignmentViewModel ToAssignmentViewModel(
        VendorCategoryAssignmentModel assignment)
    {
        return new VendorCategoryAssignmentViewModel
        {
            TicketCategoryId = assignment.TicketCategoryId,
            CategoryCode = assignment.CategoryCode,
            CategoryLabel = assignment.CategoryLabel,
            Notes = assignment.Notes,
            CreatedAt = assignment.CreatedAt
        };
    }

    private VendorRoute? BuildVendorRoute(string companySlug, Guid vendorId)
    {
        var appUserId = _portalContextResolver.Resolve().AppUserId;
        return !appUserId.HasValue || appUserId.Value == Guid.Empty
            ? null
            : new VendorRoute { AppUserId = appUserId.Value, CompanySlug = companySlug, VendorId = vendorId };
    }

    private VendorCategoryRoute? BuildVendorCategoryRoute(
        string companySlug,
        Guid vendorId,
        Guid ticketCategoryId)
    {
        var appUserId = _portalContextResolver.Resolve().AppUserId;
        return !appUserId.HasValue || appUserId.Value == Guid.Empty
            ? null
            : new VendorCategoryRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                VendorId = vendorId,
                TicketCategoryId = ticketCategoryId
            };
    }

    private void AddErrorsToModelState(IEnumerable<IError> errors, string? prefix)
    {
        foreach (var validation in errors.OfType<ValidationAppError>())
        {
            foreach (var failure in validation.Failures)
            {
                ModelState.AddModelError(
                    string.IsNullOrWhiteSpace(prefix) ? failure.PropertyName : $"{prefix}.{failure.PropertyName}",
                    failure.ErrorMessage);
            }
        }

        foreach (var error in errors.Where(error => error is not ValidationAppError))
        {
            ModelState.AddModelError(string.Empty, error.Message);
        }
    }

    private static bool HasAccessError(IEnumerable<IError> errors)
    {
        return errors.Any(error => error is UnauthorizedError or NotFoundError or ForbiddenError);
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

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}

