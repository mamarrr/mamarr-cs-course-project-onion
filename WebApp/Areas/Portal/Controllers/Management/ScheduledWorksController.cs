using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.ScheduledWorks;
using App.BLL.DTO.ScheduledWorks.Models;
using App.BLL.DTO.Tickets.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.ScheduledWorks;

namespace WebApp.Areas.Portal.Controllers.Management;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/tickets/{ticketId:guid}/scheduled-work")]
public class ScheduledWorksController : Controller
{
    private readonly IAppBLL _bll;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public ScheduledWorksController(
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
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var route = BuildTicketRoute(companySlug, ticketId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.ScheduledWorks.ListForTicketAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        var chrome = await BuildChromeAsync(
            result.Value.CompanySlug,
            result.Value.CompanyName,
            T("ScheduledWork", "Scheduled work"),
            cancellationToken);

        return View(ToIndexPage(result.Value, chrome));
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(
        string companySlug,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var page = await BuildCreatePageAsync(companySlug, ticketId, null, cancellationToken);
        return page.response ?? View(page.model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string companySlug,
        Guid ticketId,
        ScheduledWorkFormPageViewModel vm,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildCreatePageAsync(companySlug, ticketId, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        var route = BuildTicketRoute(companySlug, ticketId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.ScheduledWorks.ScheduleAsync(route, ToBllDto(vm.Form), cancellationToken);
        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            AddErrorsToModelState(result.Errors);
            var invalidPage = await BuildCreatePageAsync(companySlug, ticketId, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        TempData["ManagementScheduledWorkSuccess"] = T("ScheduledWorkCreatedSuccessfully", "Scheduled work created successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, ticketId });
    }

    [HttpGet("{scheduledWorkId:guid}")]
    public async Task<IActionResult> Details(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        CancellationToken cancellationToken)
    {
        var route = BuildScheduledWorkRoute(companySlug, ticketId, scheduledWorkId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.ScheduledWorks.GetDetailsAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        var chrome = await BuildChromeAsync(
            result.Value.CompanySlug,
            result.Value.CompanyName,
            T("ScheduledWork", "Scheduled work"),
            cancellationToken);

        return View(ToDetailsPage(result.Value, chrome));
    }

    [HttpGet("{scheduledWorkId:guid}/edit")]
    public async Task<IActionResult> Edit(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        CancellationToken cancellationToken)
    {
        var page = await BuildEditPageAsync(companySlug, ticketId, scheduledWorkId, null, cancellationToken);
        return page.response ?? View(page.model);
    }

    [HttpPost("{scheduledWorkId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        ScheduledWorkFormPageViewModel vm,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildEditPageAsync(companySlug, ticketId, scheduledWorkId, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        var route = BuildScheduledWorkRoute(companySlug, ticketId, scheduledWorkId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.ScheduledWorks.UpdateScheduleAsync(route, ToBllDto(vm.Form), cancellationToken);
        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            AddErrorsToModelState(result.Errors);
            var invalidPage = await BuildEditPageAsync(companySlug, ticketId, scheduledWorkId, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        TempData["ManagementScheduledWorkSuccess"] = T("ScheduledWorkUpdatedSuccessfully", "Scheduled work updated successfully.");
        return RedirectToAction(nameof(Details), new { companySlug, ticketId, scheduledWorkId });
    }

    [HttpGet("{scheduledWorkId:guid}/delete")]
    public async Task<IActionResult> Delete(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        CancellationToken cancellationToken)
    {
        var route = BuildScheduledWorkRoute(companySlug, ticketId, scheduledWorkId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.ScheduledWorks.GetDetailsAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        var chrome = await BuildChromeAsync(
            result.Value.CompanySlug,
            result.Value.CompanyName,
            T("DeleteScheduledWork", "Delete scheduled work"),
            cancellationToken);

        return View(new ScheduledWorkDeleteViewModel
        {
            AppChrome = chrome,
            CompanySlug = result.Value.CompanySlug,
            CompanyName = result.Value.CompanyName,
            TicketId = result.Value.TicketId,
            ScheduledWorkId = result.Value.ScheduledWorkId,
            TicketNr = result.Value.TicketNr,
            VendorName = result.Value.VendorName
        });
    }

    [HttpPost("{scheduledWorkId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        CancellationToken cancellationToken)
    {
        var route = BuildScheduledWorkRoute(companySlug, ticketId, scheduledWorkId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.ScheduledWorks.DeleteAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            TempData["ManagementScheduledWorkError"] = result.Errors.FirstOrDefault()?.Message
                                                       ?? T("UnableToDeleteScheduledWork", "Unable to delete scheduled work.");
            return RedirectToAction(nameof(Details), new { companySlug, ticketId, scheduledWorkId });
        }

        TempData["ManagementScheduledWorkSuccess"] = T("ScheduledWorkDeletedSuccessfully", "Scheduled work deleted successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, ticketId });
    }

    [HttpPost("{scheduledWorkId:guid}/start")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        ScheduledWorkDetailsViewModel vm,
        CancellationToken cancellationToken)
    {
        return await RunActionAsync(
            companySlug,
            ticketId,
            scheduledWorkId,
            route => _bll.ScheduledWorks.StartWorkAsync(route, vm.StartForm.ActionAt, cancellationToken),
            T("ScheduledWorkStartedSuccessfully", "Scheduled work started successfully."),
            T("UnableToStartScheduledWork", "Unable to start scheduled work."));
    }

    [HttpPost("{scheduledWorkId:guid}/complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        ScheduledWorkDetailsViewModel vm,
        CancellationToken cancellationToken)
    {
        return await RunActionAsync(
            companySlug,
            ticketId,
            scheduledWorkId,
            route => _bll.ScheduledWorks.CompleteWorkAsync(route, vm.CompleteForm.ActionAt, cancellationToken),
            T("ScheduledWorkCompletedSuccessfully", "Scheduled work completed successfully."),
            T("UnableToCompleteScheduledWork", "Unable to complete scheduled work."));
    }

    [HttpPost("{scheduledWorkId:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        CancellationToken cancellationToken)
    {
        return await RunActionAsync(
            companySlug,
            ticketId,
            scheduledWorkId,
            route => _bll.ScheduledWorks.CancelWorkAsync(route, cancellationToken),
            T("ScheduledWorkCancelledSuccessfully", "Scheduled work cancelled successfully."),
            T("UnableToCancelScheduledWork", "Unable to cancel scheduled work."));
    }

    private async Task<IActionResult> RunActionAsync(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        Func<ScheduledWorkRoute, Task<Result>> action,
        string successMessage,
        string errorFallback)
    {
        var route = BuildScheduledWorkRoute(companySlug, ticketId, scheduledWorkId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await action(route);
        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            TempData["ManagementScheduledWorkError"] = result.Errors.FirstOrDefault()?.Message ?? errorFallback;
            return RedirectToAction(nameof(Details), new { companySlug, ticketId, scheduledWorkId });
        }

        TempData["ManagementScheduledWorkSuccess"] = successMessage;
        return RedirectToAction(nameof(Details), new { companySlug, ticketId, scheduledWorkId });
    }

    private async Task<(IActionResult? response, ScheduledWorkFormPageViewModel? model)> BuildCreatePageAsync(
        string companySlug,
        Guid ticketId,
        ScheduledWorkFormViewModel? formOverride,
        CancellationToken cancellationToken)
    {
        var route = BuildTicketRoute(companySlug, ticketId);
        if (route is null)
        {
            return (Challenge(), null);
        }

        var result = await _bll.ScheduledWorks.GetCreateFormAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return (ToMvcErrorResult(result.Errors), null);
        }

        var chrome = await BuildChromeAsync(
            result.Value.CompanySlug,
            result.Value.CompanyName,
            T("CreateScheduledWork", "Create scheduled work"),
            cancellationToken);

        return (null, ToFormPage(result.Value, chrome, false, formOverride));
    }

    private async Task<(IActionResult? response, ScheduledWorkFormPageViewModel? model)> BuildEditPageAsync(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        ScheduledWorkFormViewModel? formOverride,
        CancellationToken cancellationToken)
    {
        var route = BuildScheduledWorkRoute(companySlug, ticketId, scheduledWorkId);
        if (route is null)
        {
            return (Challenge(), null);
        }

        var result = await _bll.ScheduledWorks.GetEditFormAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return (ToMvcErrorResult(result.Errors), null);
        }

        var chrome = await BuildChromeAsync(
            result.Value.CompanySlug,
            result.Value.CompanyName,
            T("EditScheduledWork", "Edit scheduled work"),
            cancellationToken);

        return (null, ToFormPage(result.Value, chrome, true, formOverride));
    }

    private TicketRoute? BuildTicketRoute(string companySlug, Guid ticketId)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new TicketRoute
            {
                CompanySlug = companySlug,
                TicketId = ticketId,
                AppUserId = appUserId.Value
            };
    }

    private ScheduledWorkRoute? BuildScheduledWorkRoute(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new ScheduledWorkRoute
            {
                CompanySlug = companySlug,
                TicketId = ticketId,
                ScheduledWorkId = scheduledWorkId,
                AppUserId = appUserId.Value
            };
    }

    private static ScheduledWorkBllDto ToBllDto(ScheduledWorkFormViewModel form)
    {
        return new ScheduledWorkBllDto
        {
            Id = form.ScheduledWorkId ?? Guid.Empty,
            VendorId = form.VendorId,
            WorkStatusId = form.WorkStatusId,
            ScheduledStart = form.ScheduledStart,
            ScheduledEnd = form.ScheduledEnd,
            RealStart = form.RealStart,
            RealEnd = form.RealEnd,
            Notes = form.Notes
        };
    }

    private static ScheduledWorkIndexViewModel ToIndexPage(
        ScheduledWorkListModel model,
        AppChromeViewModel chrome)
    {
        return new ScheduledWorkIndexViewModel
        {
            AppChrome = chrome,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            TicketId = model.TicketId,
            TicketNr = model.TicketNr,
            TicketTitle = model.TicketTitle,
            Items = model.Items.Select(ToItem).ToList()
        };
    }

    private static ScheduledWorkDetailsViewModel ToDetailsPage(
        ScheduledWorkDetailsModel model,
        AppChromeViewModel chrome)
    {
        return new ScheduledWorkDetailsViewModel
        {
            AppChrome = chrome,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            TicketId = model.TicketId,
            ScheduledWorkId = model.ScheduledWorkId,
            TicketNr = model.TicketNr,
            TicketTitle = model.TicketTitle,
            Item = ToItem(model),
            StartForm = new ScheduledWorkActionDateViewModel { ActionAt = DateTime.UtcNow },
            CompleteForm = new ScheduledWorkActionDateViewModel { ActionAt = DateTime.UtcNow }
        };
    }

    private static ScheduledWorkFormPageViewModel ToFormPage(
        ScheduledWorkFormModel model,
        AppChromeViewModel chrome,
        bool isEdit,
        ScheduledWorkFormViewModel? formOverride)
    {
        return new ScheduledWorkFormPageViewModel
        {
            AppChrome = chrome,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            TicketId = model.TicketId,
            TicketNr = model.TicketNr,
            TicketTitle = model.TicketTitle,
            IsEdit = isEdit,
            Form = formOverride ?? new ScheduledWorkFormViewModel
            {
                ScheduledWorkId = model.ScheduledWorkId,
                VendorId = model.VendorId,
                WorkStatusId = model.WorkStatusId,
                ScheduledStart = model.ScheduledStart,
                ScheduledEnd = model.ScheduledEnd,
                RealStart = model.RealStart,
                RealEnd = model.RealEnd,
                Notes = model.Notes
            },
            Vendors = ToSelectList(model.Vendors),
            WorkStatuses = ToSelectList(model.WorkStatuses)
        };
    }

    private static ScheduledWorkListItemViewModel ToItem(ScheduledWorkListItemModel model)
    {
        return new ScheduledWorkListItemViewModel
        {
            ScheduledWorkId = model.ScheduledWorkId,
            VendorId = model.VendorId,
            VendorName = model.VendorName,
            WorkStatusId = model.WorkStatusId,
            WorkStatusCode = model.WorkStatusCode,
            WorkStatusLabel = model.WorkStatusLabel,
            ScheduledStart = model.ScheduledStart,
            ScheduledEnd = model.ScheduledEnd,
            RealStart = model.RealStart,
            RealEnd = model.RealEnd,
            Notes = model.Notes,
            CreatedAt = model.CreatedAt,
            WorkLogCount = model.WorkLogCount
        };
    }

    private static IReadOnlyList<SelectListItem> ToSelectList(IReadOnlyList<TicketOptionModel> options)
    {
        return options
            .Select(option => new SelectListItem
            {
                Value = option.Id.ToString(),
                Text = option.Label
            })
            .ToList();
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
                ActiveSection = Sections.Tickets,
                ManagementCompanySlug = companySlug,
                ManagementCompanyName = companyName,
                CurrentLevel = WorkspaceLevel.ManagementCompany
            },
            cancellationToken);
    }

    private void AddErrorsToModelState(IReadOnlyList<IError> errors)
    {
        var validationErrors = errors.OfType<ValidationAppError>().ToList();
        foreach (var validationError in validationErrors)
        {
            foreach (var failure in validationError.Failures)
            {
                ModelState.AddModelError($"Form.{failure.PropertyName}", failure.ErrorMessage);
            }
        }

        if (validationErrors.Count > 0)
        {
            return;
        }

        var first = errors.FirstOrDefault();
        if (first is not null)
        {
            ModelState.AddModelError(string.Empty, first.Message);
        }
    }

    private static bool HasAccessError(IReadOnlyList<IError> errors)
    {
        return errors.Any(error => error is UnauthorizedError or ForbiddenError or NotFoundError);
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

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
