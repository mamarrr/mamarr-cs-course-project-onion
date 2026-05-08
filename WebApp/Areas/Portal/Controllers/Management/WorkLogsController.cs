using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.WorkLogs;
using App.BLL.DTO.WorkLogs.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.WorkLogs;

namespace WebApp.Areas.Portal.Controllers.Management;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}/work-logs")]
public class WorkLogsController : Controller
{
    private readonly IAppBLL _bll;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public WorkLogsController(
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
        Guid scheduledWorkId,
        CancellationToken cancellationToken)
    {
        var route = BuildScheduledWorkRoute(companySlug, ticketId, scheduledWorkId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.WorkLogs.ListForScheduledWorkAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        var chrome = await BuildChromeAsync(
            result.Value.CompanySlug,
            result.Value.CompanyName,
            T("WorkLogs", "Work logs"),
            cancellationToken);

        return View(ToIndexPage(result.Value, chrome));
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        CancellationToken cancellationToken)
    {
        var page = await BuildCreatePageAsync(companySlug, ticketId, scheduledWorkId, null, cancellationToken);
        return page.response ?? View(page.model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        WorkLogFormPageViewModel vm,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildCreatePageAsync(companySlug, ticketId, scheduledWorkId, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        var route = BuildScheduledWorkRoute(companySlug, ticketId, scheduledWorkId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.WorkLogs.AddAsync(route, ToBllDto(vm.Form), cancellationToken);
        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            AddErrorsToModelState(result.Errors);
            var invalidPage = await BuildCreatePageAsync(companySlug, ticketId, scheduledWorkId, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        TempData["ManagementWorkLogSuccess"] = T("WorkLogCreatedSuccessfully", "Work log added successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, ticketId, scheduledWorkId });
    }

    [HttpGet("{workLogId:guid}/edit")]
    public async Task<IActionResult> Edit(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        Guid workLogId,
        CancellationToken cancellationToken)
    {
        var page = await BuildEditPageAsync(companySlug, ticketId, scheduledWorkId, workLogId, null, cancellationToken);
        return page.response ?? View(page.model);
    }

    [HttpPost("{workLogId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        Guid workLogId,
        WorkLogFormPageViewModel vm,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildEditPageAsync(companySlug, ticketId, scheduledWorkId, workLogId, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        var route = BuildWorkLogRoute(companySlug, ticketId, scheduledWorkId, workLogId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.WorkLogs.UpdateAsync(route, ToBllDto(vm.Form), cancellationToken);
        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            AddErrorsToModelState(result.Errors);
            var invalidPage = await BuildEditPageAsync(companySlug, ticketId, scheduledWorkId, workLogId, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        TempData["ManagementWorkLogSuccess"] = T("WorkLogUpdatedSuccessfully", "Work log updated successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, ticketId, scheduledWorkId });
    }

    [HttpGet("{workLogId:guid}/delete")]
    public async Task<IActionResult> Delete(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        Guid workLogId,
        CancellationToken cancellationToken)
    {
        var route = BuildWorkLogRoute(companySlug, ticketId, scheduledWorkId, workLogId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.WorkLogs.GetDeleteModelAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        var chrome = await BuildChromeAsync(
            result.Value.CompanySlug,
            result.Value.CompanyName,
            T("DeleteWorkLog", "Delete work log"),
            cancellationToken);

        return View(ToDeletePage(result.Value, chrome));
    }

    [HttpPost("{workLogId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        Guid workLogId,
        CancellationToken cancellationToken)
    {
        var route = BuildWorkLogRoute(companySlug, ticketId, scheduledWorkId, workLogId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.WorkLogs.DeleteAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            TempData["ManagementWorkLogError"] = result.Errors.FirstOrDefault()?.Message
                                                ?? T("UnableToDeleteWorkLog", "Unable to delete work log.");
            return RedirectToAction(nameof(Index), new { companySlug, ticketId, scheduledWorkId });
        }

        TempData["ManagementWorkLogSuccess"] = T("WorkLogDeletedSuccessfully", "Work log deleted successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, ticketId, scheduledWorkId });
    }

    private async Task<(IActionResult? response, WorkLogFormPageViewModel? model)> BuildCreatePageAsync(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        WorkLogFormViewModel? formOverride,
        CancellationToken cancellationToken)
    {
        var route = BuildScheduledWorkRoute(companySlug, ticketId, scheduledWorkId);
        if (route is null)
        {
            return (Challenge(), null);
        }

        var result = await _bll.WorkLogs.GetCreateFormAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return (ToMvcErrorResult(result.Errors), null);
        }

        var chrome = await BuildChromeAsync(
            result.Value.CompanySlug,
            result.Value.CompanyName,
            T("CreateWorkLog", "Add work log"),
            cancellationToken);

        return (null, ToFormPage(result.Value, chrome, false, formOverride));
    }

    private async Task<(IActionResult? response, WorkLogFormPageViewModel? model)> BuildEditPageAsync(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        Guid workLogId,
        WorkLogFormViewModel? formOverride,
        CancellationToken cancellationToken)
    {
        var route = BuildWorkLogRoute(companySlug, ticketId, scheduledWorkId, workLogId);
        if (route is null)
        {
            return (Challenge(), null);
        }

        var result = await _bll.WorkLogs.GetEditFormAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return (ToMvcErrorResult(result.Errors), null);
        }

        var chrome = await BuildChromeAsync(
            result.Value.CompanySlug,
            result.Value.CompanyName,
            T("EditWorkLog", "Edit work log"),
            cancellationToken);

        return (null, ToFormPage(result.Value, chrome, true, formOverride));
    }

    private ScheduledWorkRoute? BuildScheduledWorkRoute(string companySlug, Guid ticketId, Guid scheduledWorkId)
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

    private WorkLogRoute? BuildWorkLogRoute(string companySlug, Guid ticketId, Guid scheduledWorkId, Guid workLogId)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new WorkLogRoute
            {
                CompanySlug = companySlug,
                TicketId = ticketId,
                ScheduledWorkId = scheduledWorkId,
                WorkLogId = workLogId,
                AppUserId = appUserId.Value
            };
    }

    private static WorkLogBllDto ToBllDto(WorkLogFormViewModel form)
    {
        return new WorkLogBllDto
        {
            Id = form.WorkLogId ?? Guid.Empty,
            WorkStart = form.WorkStart,
            WorkEnd = form.WorkEnd,
            Hours = form.Hours,
            MaterialCost = form.MaterialCost,
            LaborCost = form.LaborCost,
            Description = form.Description
        };
    }

    private static WorkLogIndexViewModel ToIndexPage(WorkLogListModel model, AppChromeViewModel chrome)
    {
        return new WorkLogIndexViewModel
        {
            AppChrome = chrome,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            TicketId = model.TicketId,
            ScheduledWorkId = model.ScheduledWorkId,
            TicketNr = model.TicketNr,
            TicketTitle = model.TicketTitle,
            VendorName = model.VendorName,
            WorkStatusLabel = model.WorkStatusLabel,
            CanViewCosts = model.CanViewCosts,
            Totals = new WorkLogTotalsViewModel
            {
                Count = model.Totals.Count,
                Hours = model.Totals.Hours,
                MaterialCost = model.Totals.MaterialCost,
                LaborCost = model.Totals.LaborCost,
                TotalCost = model.Totals.TotalCost
            },
            Items = model.Items.Select(ToItem).ToList()
        };
    }

    private static WorkLogFormPageViewModel ToFormPage(
        WorkLogFormModel model,
        AppChromeViewModel chrome,
        bool isEdit,
        WorkLogFormViewModel? formOverride)
    {
        return new WorkLogFormPageViewModel
        {
            AppChrome = chrome,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            TicketId = model.TicketId,
            ScheduledWorkId = model.ScheduledWorkId,
            TicketNr = model.TicketNr,
            TicketTitle = model.TicketTitle,
            VendorName = model.VendorName,
            CanViewCosts = model.CanViewCosts,
            IsEdit = isEdit,
            Form = formOverride ?? new WorkLogFormViewModel
            {
                WorkLogId = model.WorkLogId,
                WorkStart = model.WorkStart,
                WorkEnd = model.WorkEnd,
                Hours = model.Hours,
                MaterialCost = model.MaterialCost,
                LaborCost = model.LaborCost,
                Description = model.Description
            }
        };
    }

    private static WorkLogDeleteViewModel ToDeletePage(WorkLogDeleteModel model, AppChromeViewModel chrome)
    {
        return new WorkLogDeleteViewModel
        {
            AppChrome = chrome,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            TicketId = model.TicketId,
            ScheduledWorkId = model.ScheduledWorkId,
            WorkLogId = model.WorkLogId,
            TicketNr = model.TicketNr,
            VendorName = model.VendorName,
            Description = model.Description
        };
    }

    private static WorkLogListItemViewModel ToItem(WorkLogListItemModel model)
    {
        return new WorkLogListItemViewModel
        {
            WorkLogId = model.WorkLogId,
            AppUserName = model.AppUserName,
            WorkStart = model.WorkStart,
            WorkEnd = model.WorkEnd,
            Hours = model.Hours,
            MaterialCost = model.MaterialCost,
            LaborCost = model.LaborCost,
            Description = model.Description,
            CreatedAt = model.CreatedAt
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
