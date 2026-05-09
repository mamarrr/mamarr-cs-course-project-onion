using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
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

namespace WebApp.Areas.Portal.Controllers.Customer;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/customers/{customerSlug}/tickets")]
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

    [HttpGet("", Name = PortalRouteNames.CustomerTickets)]
    public async Task<IActionResult> Index(
        string companySlug,
        string customerSlug,
        [FromQuery] TicketFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var result = await _bll.Tickets.SearchCustomerTicketsAsync(
            ToContextTicketSearchRoute(companySlug, customerSlug, filter, appUserId.Value),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        var chrome = await _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = App.Resources.Views.UiText.Tickets,
                ActiveSection = Sections.Tickets,
                ManagementCompanySlug = result.Value.CompanySlug,
                ManagementCompanyName = result.Value.CompanyName,
                CustomerSlug = result.Value.CustomerSlug,
                CustomerName = result.Value.CustomerName,
                CurrentLevel = WorkspaceLevel.Customer
            },
            cancellationToken);

        return View(
            "~/Areas/Portal/Views/Shared/ContextTickets.cshtml",
            ContextTicketsPageMapper.ToPage(
                result.Value,
                chrome,
                PortalRouteNames.CustomerTickets,
                new Dictionary<string, string>
                {
                    ["companySlug"] = result.Value.CompanySlug,
                    ["customerSlug"] = result.Value.CustomerSlug ?? customerSlug
                },
                showCustomerFilter: false,
                showPropertyFilter: true,
                showUnitFilter: true));
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

    private Guid? GetAppUserId()
    {
        return _portalContextResolver.Resolve().AppUserId;
    }

    private static ContextTicketSearchRoute ToContextTicketSearchRoute(
        string companySlug,
        string customerSlug,
        TicketFilterViewModel filter,
        Guid appUserId)
    {
        return new ContextTicketSearchRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            Search = filter.Search,
            StatusId = filter.StatusId,
            PriorityId = filter.PriorityId,
            CategoryId = filter.CategoryId,
            PropertyId = filter.PropertyId,
            UnitId = filter.UnitId,
            VendorId = filter.VendorId,
            DueFrom = filter.DueFrom,
            DueTo = filter.DueTo
        };
    }
}
