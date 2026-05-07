using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Tickets;
using App.BLL.DTO.Tickets.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.Tickets;

namespace WebApp.Areas.Portal.Controllers.Management;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/tickets")]
public class TicketsController : Controller
{
    private readonly IAppBLL _bll;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public TicketsController(
        IAppBLL bll,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver)
    {
        _bll = bll;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
    }

    [HttpGet("", Name = PortalRouteNames.ManagementTickets)]
    public async Task<IActionResult> Index(
        string companySlug,
        [FromQuery] TicketFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var result = await _bll.Tickets.SearchAsync(
            ToListRoute(companySlug, filter, appUserId.Value),
            cancellationToken);

        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        var chrome = await BuildChromeAsync(
            result.Value.CompanySlug,
            result.Value.CompanyName,
            UiText.Tickets,
            cancellationToken);

        return View(ToPage(result.Value, chrome));
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var page = await BuildCreatePageAsync(companySlug, null, cancellationToken);
        return page.response ?? View(page.model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string companySlug,
        TicketFormPageViewModel vm,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildCreatePageAsync(companySlug, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var result = await _bll.Tickets.CreateAsync(
            ToCompanyRoute(companySlug, appUserId.Value),
            ToCreateDto(vm.Form),
            cancellationToken);

        if (result.IsFailed)
        {
            AddErrorsToModelState(result.Errors);
            if (IsAuthorizationError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            var invalidPage = await BuildCreatePageAsync(companySlug, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        TempData["ManagementTicketsSuccess"] = T("TicketCreatedSuccessfully", "Ticket created successfully.");
        return RedirectToAction(nameof(Details), new { companySlug, ticketId = result.Value.Id });
    }

    [HttpGet("{ticketId:guid}", Name = PortalRouteNames.ManagementTicketDetails)]
    public async Task<IActionResult> Details(
        string companySlug,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var result = await _bll.Tickets.GetDetailsAsync(
            ToTicketRoute(companySlug, ticketId, appUserId.Value),
            cancellationToken);

        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        var chrome = await BuildChromeAsync(
            result.Value.CompanySlug,
            result.Value.CompanyName,
            result.Value.TicketNr,
            cancellationToken);

        return View(ToDetailsPage(result.Value, chrome));
    }

    [HttpGet("{ticketId:guid}/edit")]
    public async Task<IActionResult> Edit(
        string companySlug,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var page = await BuildEditPageAsync(companySlug, ticketId, null, cancellationToken);
        return page.response ?? View(page.model);
    }

    [HttpPost("{ticketId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string companySlug,
        Guid ticketId,
        TicketFormPageViewModel vm,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildEditPageAsync(companySlug, ticketId, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var result = await _bll.Tickets.UpdateAsync(
            ToTicketRoute(companySlug, ticketId, appUserId.Value),
            ToUpdateDto(vm.Form),
            cancellationToken);

        if (result.IsFailed)
        {
            AddErrorsToModelState(result.Errors);
            if (IsAuthorizationError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            var invalidPage = await BuildEditPageAsync(companySlug, ticketId, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        TempData["ManagementTicketsSuccess"] = T("TicketUpdatedSuccessfully", "Ticket updated successfully.");
        return RedirectToAction(nameof(Details), new { companySlug, ticketId });
    }

    [HttpPost("{ticketId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string companySlug,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var result = await _bll.Tickets.DeleteAsync(
            ToTicketRoute(companySlug, ticketId, appUserId.Value),
            cancellationToken);

        if (result.IsFailed)
        {
            if (IsAuthorizationError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            TempData["ManagementTicketsError"] = result.Errors.FirstOrDefault()?.Message
                                                 ?? T("UnableToDeleteTicket", "Unable to delete ticket.");
            return RedirectToAction(nameof(Details), new { companySlug, ticketId });
        }

        TempData["ManagementTicketsSuccess"] = T("TicketDeletedSuccessfully", "Ticket deleted successfully.");
        return RedirectToAction(nameof(Index), new { companySlug });
    }

    [HttpPost("{ticketId:guid}/advance-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdvanceStatus(
        string companySlug,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var result = await _bll.Tickets.AdvanceStatusAsync(
            ToTicketRoute(companySlug, ticketId, appUserId.Value),
            cancellationToken);

        if (result.IsFailed)
        {
            if (IsAuthorizationError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            TempData["ManagementTicketsError"] = result.Errors.FirstOrDefault()?.Message
                                                 ?? T("UnableToAdvanceTicketStatus", "Unable to advance ticket status.");
            return RedirectToAction(nameof(Details), new { companySlug, ticketId });
        }

        TempData["ManagementTicketsSuccess"] = T("TicketStatusAdvancedSuccessfully", "Ticket status advanced successfully.");
        return RedirectToAction(nameof(Details), new { companySlug, ticketId });
    }

    [HttpGet("options/properties", Name = PortalRouteNames.ManagementTicketPropertyOptions)]
    public async Task<IActionResult> PropertyOptions(
        string companySlug,
        Guid? customerId,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var options = await _bll.Tickets.GetSelectorOptionsAsync(
            ToSelectorRoute(companySlug, customerId, null, null, null, appUserId.Value),
            cancellationToken);

        return options.IsFailed
            ? ToMvcErrorResult(options.Errors)
            : Json(ToJsonOptions(options.Value.Properties));
    }

    [HttpGet("options/units", Name = PortalRouteNames.ManagementTicketUnitOptions)]
    public async Task<IActionResult> UnitOptions(
        string companySlug,
        Guid? propertyId,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var options = await _bll.Tickets.GetSelectorOptionsAsync(
            ToSelectorRoute(companySlug, null, propertyId, null, null, appUserId.Value),
            cancellationToken);

        return options.IsFailed
            ? ToMvcErrorResult(options.Errors)
            : Json(ToJsonOptions(options.Value.Units));
    }

    [HttpGet("options/residents", Name = PortalRouteNames.ManagementTicketResidentOptions)]
    public async Task<IActionResult> ResidentOptions(
        string companySlug,
        Guid? unitId,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var options = await _bll.Tickets.GetSelectorOptionsAsync(
            ToSelectorRoute(companySlug, null, null, unitId, null, appUserId.Value),
            cancellationToken);

        return options.IsFailed
            ? ToMvcErrorResult(options.Errors)
            : Json(ToJsonOptions(options.Value.Residents));
    }

    [HttpGet("options/vendors", Name = PortalRouteNames.ManagementTicketVendorOptions)]
    public async Task<IActionResult> VendorOptions(
        string companySlug,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var options = await _bll.Tickets.GetSelectorOptionsAsync(
            ToSelectorRoute(companySlug, null, null, null, categoryId, appUserId.Value),
            cancellationToken);

        return options.IsFailed
            ? ToMvcErrorResult(options.Errors)
            : Json(ToJsonOptions(options.Value.Vendors));
    }

    private static ManagementTicketSearchRoute ToListRoute(
        string companySlug,
        TicketFilterViewModel filter,
        Guid appUserId)
    {
        return new ManagementTicketSearchRoute
        {
            CompanySlug = companySlug,
            AppUserId = appUserId,
            Search = filter.Search,
            StatusId = filter.StatusId,
            PriorityId = filter.PriorityId,
            CategoryId = filter.CategoryId,
            CustomerId = filter.CustomerId,
            PropertyId = filter.PropertyId,
            UnitId = filter.UnitId,
            VendorId = filter.VendorId,
            DueFrom = filter.DueFrom,
            DueTo = filter.DueTo
        };
    }

    private static TicketRoute ToTicketRoute(string companySlug, Guid ticketId, Guid appUserId)
    {
        return new TicketRoute
        {
            CompanySlug = companySlug,
            TicketId = ticketId,
            AppUserId = appUserId
        };
    }

    private static TicketSelectorOptionsRoute ToSelectorRoute(
        string companySlug,
        Guid? customerId,
        Guid? propertyId,
        Guid? unitId,
        Guid? categoryId,
        Guid appUserId)
    {
        return new TicketSelectorOptionsRoute
        {
            CompanySlug = companySlug,
            AppUserId = appUserId,
            CustomerId = customerId,
            PropertyId = propertyId,
            UnitId = unitId,
            CategoryId = categoryId
        };
    }

    private static ManagementCompanyRoute ToCompanyRoute(string companySlug, Guid appUserId)
    {
        return new ManagementCompanyRoute
        {
            CompanySlug = companySlug,
            AppUserId = appUserId
        };
    }

    private static TicketBllDto ToCreateDto(TicketFormViewModel form)
    {
        return new TicketBllDto
        {
            TicketNr = form.TicketNr,
            Title = form.Title,
            Description = form.Description,
            TicketCategoryId = form.TicketCategoryId,
            TicketPriorityId = form.TicketPriorityId,
            CustomerId = form.CustomerId,
            PropertyId = form.PropertyId,
            UnitId = form.UnitId,
            ResidentId = form.ResidentId,
            VendorId = form.VendorId,
            DueAt = form.DueAt
        };
    }

    private static TicketBllDto ToUpdateDto(TicketFormViewModel form)
    {
        return new TicketBllDto
        {
            TicketNr = form.TicketNr,
            Title = form.Title,
            Description = form.Description,
            TicketCategoryId = form.TicketCategoryId,
            TicketStatusId = form.TicketStatusId,
            TicketPriorityId = form.TicketPriorityId,
            CustomerId = form.CustomerId,
            PropertyId = form.PropertyId,
            UnitId = form.UnitId,
            ResidentId = form.ResidentId,
            VendorId = form.VendorId,
            DueAt = form.DueAt
        };
    }

    private static TicketsPageViewModel ToPage(
        ManagementTicketsModel model,
        AppChromeViewModel chrome)
    {
        return new TicketsPageViewModel
        {
            AppChrome = chrome,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            Filter = new TicketFilterViewModel
            {
                Search = model.Filter.Search,
                StatusId = model.Filter.StatusId,
                PriorityId = model.Filter.PriorityId,
                CategoryId = model.Filter.CategoryId,
                CustomerId = model.Filter.CustomerId,
                PropertyId = model.Filter.PropertyId,
                UnitId = model.Filter.UnitId,
                VendorId = model.Filter.VendorId,
                DueFrom = model.Filter.DueFrom,
                DueTo = model.Filter.DueTo
            },
            Tickets = model.Tickets.Select(ToListItem).ToList(),
            Options = ToOptions(model.Options)
        };
    }

    private static TicketFormPageViewModel ToFormPage(
        ManagementTicketFormModel model,
        AppChromeViewModel chrome,
        bool isEdit,
        TicketFormViewModel? formOverride = null)
    {
        return new TicketFormPageViewModel
        {
            AppChrome = chrome,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            IsEdit = isEdit,
            Form = formOverride ?? new TicketFormViewModel
            {
                TicketId = model.TicketId,
                TicketNr = model.TicketNr,
                Title = model.Title,
                Description = model.Description,
                TicketCategoryId = model.TicketCategoryId,
                TicketStatusId = model.TicketStatusId,
                TicketPriorityId = model.TicketPriorityId,
                CustomerId = model.CustomerId,
                PropertyId = model.PropertyId,
                UnitId = model.UnitId,
                ResidentId = model.ResidentId,
                VendorId = model.VendorId,
                DueAt = model.DueAt
            },
            Options = ToOptions(model.Options)
        };
    }

    private static TicketDetailsPageViewModel ToDetailsPage(
        ManagementTicketDetailsModel model,
        AppChromeViewModel chrome)
    {
        return new TicketDetailsPageViewModel
        {
            AppChrome = chrome,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            TicketId = model.TicketId,
            TicketNr = model.TicketNr,
            Title = model.Title,
            Description = model.Description,
            StatusCode = model.StatusCode,
            StatusLabel = model.StatusLabel,
            PriorityLabel = model.PriorityLabel,
            CategoryLabel = model.CategoryLabel,
            CustomerName = model.CustomerName,
            CustomerSlug = model.CustomerSlug,
            PropertyName = model.PropertyName,
            PropertySlug = model.PropertySlug,
            UnitNr = model.UnitNr,
            UnitSlug = model.UnitSlug,
            ResidentName = model.ResidentName,
            ResidentIdCode = model.ResidentIdCode,
            VendorName = model.VendorName,
            CreatedAt = model.CreatedAt,
            DueAt = model.DueAt,
            ClosedAt = model.ClosedAt,
            NextStatusCode = model.NextStatusCode,
            NextStatusLabel = model.NextStatusLabel
        };
    }

    private static TicketSelectOptionsViewModel ToOptions(TicketSelectorOptionsModel model)
    {
        return new TicketSelectOptionsViewModel
        {
            Statuses = ToSelectList(model.Statuses),
            Priorities = ToSelectList(model.Priorities),
            Categories = ToSelectList(model.Categories),
            Customers = ToSelectList(model.Customers),
            Properties = ToSelectList(model.Properties),
            Units = ToSelectList(model.Units),
            Residents = ToSelectList(model.Residents),
            Vendors = ToSelectList(model.Vendors)
        };
    }

    private static IReadOnlyList<TicketOptionJsonViewModel> ToJsonOptions(IReadOnlyList<TicketOptionModel> options)
    {
        return options
            .Select(option => new TicketOptionJsonViewModel
            {
                Id = option.Id,
                Label = option.Label
            })
            .ToList();
    }

    private static TicketListItemViewModel ToListItem(ManagementTicketListItemModel model)
    {
        return new TicketListItemViewModel
        {
            TicketId = model.TicketId,
            TicketNr = model.TicketNr,
            Title = model.Title,
            StatusCode = model.StatusCode,
            StatusLabel = model.StatusLabel,
            PriorityLabel = model.PriorityLabel,
            CategoryLabel = model.CategoryLabel,
            CustomerName = model.CustomerName,
            CustomerSlug = model.CustomerSlug,
            PropertyName = model.PropertyName,
            PropertySlug = model.PropertySlug,
            UnitNr = model.UnitNr,
            UnitSlug = model.UnitSlug,
            ResidentName = model.ResidentName,
            ResidentIdCode = model.ResidentIdCode,
            VendorName = model.VendorName,
            DueAt = model.DueAt,
            CreatedAt = model.CreatedAt
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

    private async Task<(IActionResult? response, TicketFormPageViewModel? model)> BuildCreatePageAsync(
        string companySlug,
        TicketFormViewModel? formOverride,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return (Challenge(), null);
        }

        var result = await _bll.Tickets.GetCreateFormAsync(
            ToSelectorRoute(
                companySlug,
                formOverride?.CustomerId,
                formOverride?.PropertyId,
                formOverride?.UnitId,
                formOverride?.TicketCategoryId == Guid.Empty ? null : formOverride?.TicketCategoryId,
                appUserId.Value),
            cancellationToken);

        if (result.IsFailed)
        {
            return (ToMvcErrorResult(result.Errors), null);
        }

        var chrome = await BuildChromeAsync(
            result.Value.CompanySlug,
            result.Value.CompanyName,
            T("CreateTicket", "Create ticket"),
            cancellationToken);

        return (null, ToFormPage(result.Value, chrome, isEdit: false, formOverride));
    }

    private async Task<(IActionResult? response, TicketFormPageViewModel? model)> BuildEditPageAsync(
        string companySlug,
        Guid ticketId,
        TicketFormViewModel? formOverride,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return (Challenge(), null);
        }

        var result = await _bll.Tickets.GetEditFormAsync(
            ToTicketRoute(companySlug, ticketId, appUserId.Value),
            cancellationToken);

        if (result.IsFailed)
        {
            return (ToMvcErrorResult(result.Errors), null);
        }

        var chrome = await BuildChromeAsync(
            result.Value.CompanySlug,
            result.Value.CompanyName,
            T("EditTicket", "Edit ticket"),
            cancellationToken);

        return (null, ToFormPage(result.Value, chrome, isEdit: true, formOverride));
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
        if (first is ConflictError)
        {
            ModelState.AddModelError("Form.TicketNr", first.Message);
        }
        else if (first is not null)
        {
            ModelState.AddModelError(string.Empty, first.Message);
        }
    }

    private static bool IsAuthorizationError(IReadOnlyList<IError> errors)
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
