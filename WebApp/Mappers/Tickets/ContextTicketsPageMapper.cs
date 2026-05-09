using App.BLL.DTO.Tickets.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.UI.Chrome;
using WebApp.ViewModels.Management.Tickets;

namespace WebApp.Mappers.Tickets;

public static class ContextTicketsPageMapper
{
    public static ContextTicketsPageViewModel ToPage(
        ContextTicketsModel model,
        AppChromeViewModel chrome,
        string clearRouteName,
        IDictionary<string, string> clearRouteValues,
        bool showCustomerFilter,
        bool showPropertyFilter,
        bool showUnitFilter)
    {
        return new ContextTicketsPageViewModel
        {
            AppChrome = chrome,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            ContextName = model.ContextName,
            ClearRouteName = clearRouteName,
            ClearRouteValues = clearRouteValues,
            ShowCustomerFilter = showCustomerFilter,
            ShowPropertyFilter = showPropertyFilter,
            ShowUnitFilter = showUnitFilter,
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
}
