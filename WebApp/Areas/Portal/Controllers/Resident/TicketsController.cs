using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers.Tickets;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.Tickets;

namespace WebApp.Areas.Portal.Controllers.Resident;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/r/{residentIdCode}/tickets")]
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

    [HttpGet("", Name = PortalRouteNames.ResidentTickets)]
    public async Task<IActionResult> Index(
        string companySlug,
        string residentIdCode,
        [FromQuery] TicketFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var result = await _bll.Tickets.SearchResidentTicketsAsync(
            ToContextTicketSearchRoute(companySlug, residentIdCode, filter, appUserId.Value),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToFailureResult(result.Errors);
        }

        var chrome = await _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = UiText.Tickets,
                ActiveSection = Sections.Tickets,
                ManagementCompanySlug = result.Value.CompanySlug,
                ManagementCompanyName = result.Value.CompanyName,
                ResidentIdCode = result.Value.ResidentIdCode,
                ResidentDisplayName = result.Value.ResidentName,
                ResidentSupportingText = string.Equals(result.Value.ResidentName, result.Value.ResidentIdCode, StringComparison.Ordinal)
                    ? null
                    : result.Value.ResidentIdCode,
                CurrentLevel = WorkspaceLevel.Resident
            },
            cancellationToken);

        return View(
            "~/Areas/Portal/Views/Shared/ContextTickets.cshtml",
            ContextTicketsPageMapper.ToPage(
                result.Value,
                chrome,
                PortalRouteNames.ResidentTickets,
                new Dictionary<string, string>
                {
                    ["companySlug"] = result.Value.CompanySlug,
                    ["residentIdCode"] = result.Value.ResidentIdCode ?? residentIdCode
                },
                showCustomerFilter: false,
                showPropertyFilter: false,
                showUnitFilter: false));
    }

    private IActionResult ToFailureResult(IReadOnlyList<IError> errors)
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

    private Guid? GetAppUserId()
    {
        return _portalContextResolver.Resolve().AppUserId;
    }

    private static ContextTicketSearchRoute ToContextTicketSearchRoute(
        string companySlug,
        string residentIdCode,
        TicketFilterViewModel filter,
        Guid appUserId)
    {
        return new ContextTicketSearchRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode,
            Search = filter.Search,
            StatusId = filter.StatusId,
            PriorityId = filter.PriorityId,
            CategoryId = filter.CategoryId,
            VendorId = filter.VendorId,
            DueFrom = filter.DueFrom,
            DueTo = filter.DueTo
        };
    }
}
