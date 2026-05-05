using System.Globalization;
using System.Security.Claims;
using App.BLL.Contracts.Tickets.Commands;
using App.BLL.Contracts.Tickets.Models;
using App.BLL.Contracts.Tickets.Queries;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.ViewModels.Management.Tickets;

namespace WebApp.Mappers.Mvc.Tickets;

public class ManagementTicketMvcMapper
{
    public GetManagementTicketsQuery ToListQuery(
        string companySlug,
        TicketFilterViewModel filter,
        ClaimsPrincipal user)
    {
        return new GetManagementTicketsQuery
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
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

    public GetManagementTicketQuery ToTicketQuery(
        string companySlug,
        Guid ticketId,
        ClaimsPrincipal user)
    {
        return new GetManagementTicketQuery
        {
            CompanySlug = companySlug,
            TicketId = ticketId,
            UserId = GetAppUserId(user)
        };
    }

    public GetManagementTicketSelectorOptionsQuery ToSelectorQuery(
        string companySlug,
        Guid? customerId,
        Guid? propertyId,
        Guid? unitId,
        Guid? categoryId,
        ClaimsPrincipal user)
    {
        return new GetManagementTicketSelectorOptionsQuery
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
            CustomerId = customerId,
            PropertyId = propertyId,
            UnitId = unitId,
            CategoryId = categoryId
        };
    }

    public CreateManagementTicketCommand ToCreateCommand(
        string companySlug,
        TicketFormViewModel form,
        ClaimsPrincipal user)
    {
        return new CreateManagementTicketCommand
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
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

    public UpdateManagementTicketCommand ToUpdateCommand(
        string companySlug,
        Guid ticketId,
        TicketFormViewModel form,
        ClaimsPrincipal user)
    {
        return new UpdateManagementTicketCommand
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
            TicketId = ticketId,
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

    public DeleteManagementTicketCommand ToDeleteCommand(
        string companySlug,
        Guid ticketId,
        ClaimsPrincipal user)
    {
        return new DeleteManagementTicketCommand
        {
            CompanySlug = companySlug,
            TicketId = ticketId,
            UserId = GetAppUserId(user)
        };
    }

    public AdvanceManagementTicketStatusCommand ToAdvanceCommand(
        string companySlug,
        Guid ticketId,
        ClaimsPrincipal user)
    {
        return new AdvanceManagementTicketStatusCommand
        {
            CompanySlug = companySlug,
            TicketId = ticketId,
            UserId = GetAppUserId(user)
        };
    }

    public TicketsPageViewModel ToPage(
        ManagementTicketsModel model,
        WebApp.UI.Chrome.AppChromeViewModel chrome)
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

    public TicketFormPageViewModel ToFormPage(
        ManagementTicketFormModel model,
        WebApp.UI.Chrome.AppChromeViewModel chrome,
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

    public TicketDetailsPageViewModel ToDetailsPage(
        ManagementTicketDetailsModel model,
        WebApp.UI.Chrome.AppChromeViewModel chrome)
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

    public TicketSelectOptionsViewModel ToOptions(TicketSelectorOptionsModel model)
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

    public IReadOnlyList<TicketOptionJsonViewModel> ToJsonOptions(IReadOnlyList<TicketOptionModel> options)
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

    private static Guid GetAppUserId(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : Guid.Empty;
    }
}
