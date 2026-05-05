using App.BLL.Contracts;
using App.BLL.Contracts.Common.Errors;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers.Mvc.Tickets;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.Tickets;

namespace WebApp.Areas.Portal.Controllers.Management;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/tickets")]
public class TicketsController : Controller
{
    private readonly IAppBLL _bll;
    private readonly ManagementTicketMvcMapper _mapper;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public TicketsController(
        IAppBLL bll,
        ManagementTicketMvcMapper mapper,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver)
    {
        _bll = bll;
        _mapper = mapper;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
    }

    [HttpGet("")]
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

        var result = await _bll.ManagementTickets.GetTicketsAsync(
            _mapper.ToListQuery(companySlug, filter, appUserId.Value),
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

        return View(_mapper.ToPage(result.Value, chrome));
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

        var result = await _bll.ManagementTickets.CreateAsync(
            _mapper.ToCreateCommand(
                companySlug,
                vm.Form,
                appUserId.Value),
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
        return RedirectToAction(nameof(Details), new { companySlug, ticketId = result.Value });
    }

    [HttpGet("{ticketId:guid}")]
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

        var result = await _bll.ManagementTickets.GetDetailsAsync(
            _mapper.ToTicketQuery(companySlug, ticketId, appUserId.Value),
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

        return View(_mapper.ToDetailsPage(result.Value, chrome));
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

        var result = await _bll.ManagementTickets.UpdateAsync(
            _mapper.ToUpdateCommand(
                companySlug,
                ticketId,
                vm.Form,
                appUserId.Value),
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

        var result = await _bll.ManagementTickets.DeleteAsync(
            _mapper.ToDeleteCommand(companySlug, ticketId, appUserId.Value),
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

        var result = await _bll.ManagementTickets.AdvanceStatusAsync(
            _mapper.ToAdvanceCommand(companySlug, ticketId, appUserId.Value),
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

    [HttpGet("options/properties")]
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

        var options = await _bll.ManagementTickets.GetSelectorOptionsAsync(
            _mapper.ToSelectorQuery(companySlug, customerId, null, null, null, appUserId.Value),
            cancellationToken);

        return options.IsFailed
            ? ToMvcErrorResult(options.Errors)
            : Json(_mapper.ToJsonOptions(options.Value.Properties));
    }

    [HttpGet("options/units")]
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

        var options = await _bll.ManagementTickets.GetSelectorOptionsAsync(
            _mapper.ToSelectorQuery(companySlug, null, propertyId, null, null, appUserId.Value),
            cancellationToken);

        return options.IsFailed
            ? ToMvcErrorResult(options.Errors)
            : Json(_mapper.ToJsonOptions(options.Value.Units));
    }

    [HttpGet("options/residents")]
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

        var options = await _bll.ManagementTickets.GetSelectorOptionsAsync(
            _mapper.ToSelectorQuery(companySlug, null, null, unitId, null, appUserId.Value),
            cancellationToken);

        return options.IsFailed
            ? ToMvcErrorResult(options.Errors)
            : Json(_mapper.ToJsonOptions(options.Value.Residents));
    }

    [HttpGet("options/vendors")]
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

        var options = await _bll.ManagementTickets.GetSelectorOptionsAsync(
            _mapper.ToSelectorQuery(companySlug, null, null, null, categoryId, appUserId.Value),
            cancellationToken);

        return options.IsFailed
            ? ToMvcErrorResult(options.Errors)
            : Json(_mapper.ToJsonOptions(options.Value.Vendors));
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

        var result = await _bll.ManagementTickets.GetCreateFormAsync(
            _mapper.ToListQuery(
                companySlug,
                new TicketFilterViewModel
                {
                    CustomerId = formOverride?.CustomerId,
                    PropertyId = formOverride?.PropertyId,
                    UnitId = formOverride?.UnitId,
                    CategoryId = formOverride?.TicketCategoryId == Guid.Empty ? null : formOverride?.TicketCategoryId
                },
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

        return (null, _mapper.ToFormPage(result.Value, chrome, isEdit: false, formOverride));
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

        var result = await _bll.ManagementTickets.GetEditFormAsync(
            _mapper.ToTicketQuery(companySlug, ticketId, appUserId.Value),
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

        return (null, _mapper.ToFormPage(result.Value, chrome, isEdit: true, formOverride));
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
