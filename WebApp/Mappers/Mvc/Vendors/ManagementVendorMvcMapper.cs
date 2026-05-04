using System.Globalization;
using System.Security.Claims;
using App.BLL.Contracts.Vendors.Commands;
using App.BLL.Contracts.Vendors.Models;
using App.BLL.Contracts.Vendors.Queries;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.ViewModels.Management.Vendors;

namespace WebApp.Mappers.Mvc.Vendors;

public class ManagementVendorMvcMapper
{
    public GetManagementVendorsQuery ToListQuery(
        string companySlug,
        VendorFilterViewModel filter,
        ClaimsPrincipal user)
    {
        return new GetManagementVendorsQuery
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
            Search = filter.Search,
            IncludeInactive = filter.IncludeInactive,
            TicketCategoryId = filter.TicketCategoryId
        };
    }

    public GetManagementVendorQuery ToDetailsQuery(
        string companySlug,
        Guid vendorId,
        string? ticketSearch,
        ClaimsPrincipal user)
    {
        return new GetManagementVendorQuery
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
            VendorId = vendorId,
            TicketSearch = ticketSearch
        };
    }

    public CreateManagementVendorCommand ToCreateCommand(
        string companySlug,
        VendorFormViewModel form,
        ClaimsPrincipal user)
    {
        return new CreateManagementVendorCommand
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
            Name = form.Name,
            RegistryCode = form.RegistryCode,
            Notes = form.Notes,
            Culture = CurrentCultureName(),
            IsActive = form.IsActive
        };
    }

    public UpdateManagementVendorCommand ToUpdateCommand(
        string companySlug,
        Guid vendorId,
        VendorFormViewModel form,
        ClaimsPrincipal user)
    {
        return new UpdateManagementVendorCommand
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
            VendorId = vendorId,
            Name = form.Name,
            RegistryCode = form.RegistryCode,
            Notes = form.Notes,
            Culture = CurrentCultureName(),
            IsActive = form.IsActive
        };
    }

    public AddVendorCategoryCommand ToAddCategoryCommand(
        string companySlug,
        Guid vendorId,
        AddVendorCategoryViewModel form,
        ClaimsPrincipal user)
    {
        return new AddVendorCategoryCommand
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
            VendorId = vendorId,
            TicketCategoryId = form.TicketCategoryId
        };
    }

    public AddVendorContactCommand ToAddContactCommand(
        string companySlug,
        Guid vendorId,
        AddVendorContactViewModel form,
        ClaimsPrincipal user)
    {
        return new AddVendorContactCommand
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
            VendorId = vendorId,
            ContactTypeId = form.ContactTypeId,
            ContactValue = form.ContactValue,
            ContactNotes = form.ContactNotes,
            FullName = form.FullName,
            RoleTitle = form.RoleTitle,
            Culture = CurrentCultureName(),
            ValidFrom = form.ValidFrom,
            ValidTo = form.ValidTo,
            Confirmed = form.Confirmed,
            IsPrimary = form.IsPrimary
        };
    }

    public AssignVendorTicketCommand ToAssignTicketCommand(
        string companySlug,
        Guid vendorId,
        AssignVendorTicketViewModel form,
        ClaimsPrincipal user)
    {
        return new AssignVendorTicketCommand
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
            VendorId = vendorId,
            TicketId = form.TicketId
        };
    }

    public AddVendorScheduledWorkCommand ToScheduledWorkCommand(
        string companySlug,
        Guid vendorId,
        AddVendorScheduledWorkViewModel form,
        ClaimsPrincipal user)
    {
        return new AddVendorScheduledWorkCommand
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
            VendorId = vendorId,
            TicketId = form.TicketId,
            WorkStatusId = form.WorkStatusId,
            ScheduledStart = form.ScheduledStart,
            ScheduledEnd = form.ScheduledEnd,
            Notes = form.Notes,
            Culture = CurrentCultureName()
        };
    }

    public VendorsPageViewModel ToPage(
        ManagementVendorsModel model,
        WebApp.UI.Chrome.AppChromeViewModel chrome,
        VendorFormViewModel? formOverride = null)
    {
        return new VendorsPageViewModel
        {
            AppChrome = chrome,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            Filter = new VendorFilterViewModel
            {
                Search = model.Filter.Search,
                IncludeInactive = model.Filter.IncludeInactive,
                TicketCategoryId = model.Filter.TicketCategoryId
            },
            Form = formOverride ?? new VendorFormViewModel(),
            Vendors = model.Vendors.Select(ToListItem).ToList(),
            Options = ToOptions(model.Options)
        };
    }

    public VendorDetailsPageViewModel ToDetailsPage(
        ManagementVendorDetailsModel model,
        WebApp.UI.Chrome.AppChromeViewModel chrome,
        VendorFormViewModel? formOverride = null)
    {
        var assignableTickets = model.AssignableTickets
            .Select(ticket => new SelectListItem
            {
                Value = ticket.TicketId.ToString(),
                Text = ticket.Label
            })
            .ToList();

        return new VendorDetailsPageViewModel
        {
            AppChrome = chrome,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            VendorId = model.VendorId,
            Name = model.Name,
            RegistryCode = model.RegistryCode,
            Notes = model.Notes,
            IsActive = model.IsActive,
            Form = formOverride ?? new VendorFormViewModel
            {
                Name = model.Name,
                RegistryCode = model.RegistryCode,
                Notes = model.Notes,
                IsActive = model.IsActive
            },
            Categories = model.Categories.Select(ToCategory).ToList(),
            Tickets = model.Tickets.Select(ToTicket).ToList(),
            Contacts = model.Contacts.Select(ToContact).ToList(),
            ScheduledWorks = model.ScheduledWorks.Select(ToScheduledWork).ToList(),
            AssignableTickets = assignableTickets,
            ScheduledWorkForm = new AddVendorScheduledWorkViewModel
            {
                ScheduledStart = DateTime.Now.AddHours(1)
            },
            Options = ToOptions(model.Options)
        };
    }

    private static VendorListItemViewModel ToListItem(VendorListItemModel model)
    {
        return new VendorListItemViewModel
        {
            VendorId = model.VendorId,
            Name = model.Name,
            RegistryCode = model.RegistryCode,
            Notes = model.Notes,
            IsActive = model.IsActive,
            ActiveCategoryCount = model.ActiveCategoryCount,
            AssignedTicketCount = model.AssignedTicketCount,
            ContactCount = model.ContactCount,
            CreatedAt = model.CreatedAt
        };
    }

    private static VendorCategoryViewModel ToCategory(VendorCategoryModel model)
    {
        return new VendorCategoryViewModel
        {
            TicketCategoryId = model.TicketCategoryId,
            Code = model.Code,
            Label = model.Label,
            IsActive = model.IsActive
        };
    }

    private static VendorTicketViewModel ToTicket(VendorTicketModel model)
    {
        return new VendorTicketViewModel
        {
            TicketId = model.TicketId,
            TicketNr = model.TicketNr,
            Title = model.Title,
            StatusLabel = model.StatusLabel,
            CategoryLabel = model.CategoryLabel,
            DueAt = model.DueAt
        };
    }

    private static VendorContactViewModel ToContact(VendorContactModel model)
    {
        return new VendorContactViewModel
        {
            ContactTypeLabel = model.ContactTypeLabel,
            ContactValue = model.ContactValue,
            FullName = model.FullName,
            RoleTitle = model.RoleTitle,
            IsPrimary = model.IsPrimary,
            Confirmed = model.Confirmed,
            ValidFrom = model.ValidFrom,
            ValidTo = model.ValidTo
        };
    }

    private static VendorScheduledWorkViewModel ToScheduledWork(VendorScheduledWorkModel model)
    {
        return new VendorScheduledWorkViewModel
        {
            TicketId = model.TicketId,
            TicketNr = model.TicketNr,
            TicketTitle = model.TicketTitle,
            WorkStatusLabel = model.WorkStatusLabel,
            ScheduledStart = model.ScheduledStart,
            ScheduledEnd = model.ScheduledEnd,
            Notes = model.Notes
        };
    }

    private static VendorOptionsViewModel ToOptions(VendorOptionsModel model)
    {
        return new VendorOptionsViewModel
        {
            TicketCategories = ToSelectList(model.TicketCategories),
            ContactTypes = ToSelectList(model.ContactTypes),
            WorkStatuses = ToSelectList(model.WorkStatuses)
        };
    }

    private static IReadOnlyList<SelectListItem> ToSelectList(IReadOnlyList<VendorOptionModel> options)
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

    private static string CurrentCultureName()
    {
        return CultureInfo.CurrentUICulture.Name;
    }
}
