using App.BLL.DTO.ScheduledWorks.Models;
using App.BLL.DTO.Tickets;
using App.BLL.DTO.Tickets.Models;
using App.DTO.v1.Portal.Tickets;
using App.DTO.v1.Shared;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.Tickets;

public class TicketApiMapper :
    IBaseMapper<CreateTicketDto, TicketBllDto>,
    IBaseMapper<UpdateTicketDto, TicketBllDto>,
    IBaseMapper<TicketDto, TicketBllDto>
{
    public TicketBllDto? Map(CreateTicketDto? entity)
    {
        return entity is null
            ? null
            : new TicketBllDto
            {
                TicketNr = entity.TicketNr,
                Title = entity.Title,
                Description = entity.Description,
                TicketCategoryId = entity.TicketCategoryId,
                TicketPriorityId = entity.TicketPriorityId,
                CustomerId = entity.CustomerId,
                PropertyId = entity.PropertyId,
                UnitId = entity.UnitId,
                ResidentId = entity.ResidentId,
                VendorId = entity.VendorId,
                DueAt = entity.DueAt
            };
    }

    public TicketBllDto? Map(UpdateTicketDto? entity)
    {
        return entity is null
            ? null
            : new TicketBllDto
            {
                TicketNr = entity.TicketNr,
                Title = entity.Title,
                Description = entity.Description,
                TicketCategoryId = entity.TicketCategoryId,
                TicketStatusId = entity.TicketStatusId,
                TicketPriorityId = entity.TicketPriorityId,
                CustomerId = entity.CustomerId,
                PropertyId = entity.PropertyId,
                UnitId = entity.UnitId,
                ResidentId = entity.ResidentId,
                VendorId = entity.VendorId,
                DueAt = entity.DueAt
            };
    }

    public TicketDto? Map(TicketBllDto? entity)
    {
        return entity is null
            ? null
            : new TicketDto
            {
                TicketId = entity.Id,
                ManagementCompanyId = entity.ManagementCompanyId,
                TicketNr = entity.TicketNr,
                Title = entity.Title,
                Description = entity.Description,
                TicketCategoryId = entity.TicketCategoryId,
                TicketStatusId = entity.TicketStatusId,
                TicketPriorityId = entity.TicketPriorityId,
                CustomerId = entity.CustomerId,
                PropertyId = entity.PropertyId,
                UnitId = entity.UnitId,
                ResidentId = entity.ResidentId,
                VendorId = entity.VendorId,
                DueAt = entity.DueAt,
                ClosedAt = entity.ClosedAt
            };
    }

    public TicketBllDto? Map(TicketDto? entity)
    {
        return entity is null
            ? null
            : new TicketBllDto
            {
                Id = entity.TicketId,
                ManagementCompanyId = entity.ManagementCompanyId,
                TicketNr = entity.TicketNr,
                Title = entity.Title,
                Description = entity.Description,
                TicketCategoryId = entity.TicketCategoryId,
                TicketStatusId = entity.TicketStatusId,
                TicketPriorityId = entity.TicketPriorityId,
                CustomerId = entity.CustomerId,
                PropertyId = entity.PropertyId,
                UnitId = entity.UnitId,
                ResidentId = entity.ResidentId,
                VendorId = entity.VendorId,
                DueAt = entity.DueAt,
                ClosedAt = entity.ClosedAt
            };
    }

    CreateTicketDto? IBaseMapper<CreateTicketDto, TicketBllDto>.Map(TicketBllDto? entity)
    {
        return entity is null
            ? null
            : new CreateTicketDto
            {
                TicketNr = entity.TicketNr,
                Title = entity.Title,
                Description = entity.Description,
                TicketCategoryId = entity.TicketCategoryId,
                TicketPriorityId = entity.TicketPriorityId,
                CustomerId = entity.CustomerId,
                PropertyId = entity.PropertyId,
                UnitId = entity.UnitId,
                ResidentId = entity.ResidentId,
                VendorId = entity.VendorId,
                DueAt = entity.DueAt
            };
    }

    UpdateTicketDto? IBaseMapper<UpdateTicketDto, TicketBllDto>.Map(TicketBllDto? entity)
    {
        return entity is null
            ? null
            : new UpdateTicketDto
            {
                TicketNr = entity.TicketNr,
                Title = entity.Title,
                Description = entity.Description,
                TicketCategoryId = entity.TicketCategoryId,
                TicketStatusId = entity.TicketStatusId,
                TicketPriorityId = entity.TicketPriorityId,
                CustomerId = entity.CustomerId,
                PropertyId = entity.PropertyId,
                UnitId = entity.UnitId,
                ResidentId = entity.ResidentId,
                VendorId = entity.VendorId,
                DueAt = entity.DueAt
            };
    }

    public ManagementTicketsDto Map(ManagementTicketsModel model)
    {
        return new ManagementTicketsDto
        {
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            Tickets = model.Tickets.Select(MapListItem).ToList(),
            Filter = MapFilter(model.Filter),
            Options = MapOptions(model.Options)
        };
    }

    public ContextTicketsDto Map(ContextTicketsModel model)
    {
        return new ContextTicketsDto
        {
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            ContextName = model.ContextName,
            CustomerSlug = model.CustomerSlug,
            CustomerName = model.CustomerName,
            PropertySlug = model.PropertySlug,
            PropertyName = model.PropertyName,
            UnitSlug = model.UnitSlug,
            UnitName = model.UnitName,
            ResidentIdCode = model.ResidentIdCode,
            ResidentName = model.ResidentName,
            Tickets = model.Tickets.Select(MapListItem).ToList(),
            Filter = MapFilter(model.Filter),
            Options = MapOptions(model.Options)
        };
    }

    public TicketDetailsDto Map(ManagementTicketDetailsModel model)
    {
        return new TicketDetailsDto
        {
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
            NextStatusLabel = model.NextStatusLabel,
            CanAdvanceStatus = model.CanAdvanceStatus,
            TransitionBlockingReasons = model.TransitionBlockingReasons,
            ScheduledWork = model.ScheduledWork.Select(MapScheduledWork).ToList()
        };
    }

    public TicketFormDto Map(ManagementTicketFormModel model)
    {
        return new TicketFormDto
        {
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
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
            DueAt = model.DueAt,
            Options = MapOptions(model.Options)
        };
    }

    public TicketSelectorOptionsDto Map(TicketSelectorOptionsModel model)
    {
        return MapOptions(model);
    }

    public TicketTransitionAvailabilityDto Map(TicketTransitionAvailabilityModel model)
    {
        return new TicketTransitionAvailabilityDto
        {
            TicketId = model.TicketId,
            CurrentStatusCode = model.CurrentStatusCode,
            NextStatusCode = model.NextStatusCode,
            NextStatusLabel = model.NextStatusLabel,
            CanAdvance = model.CanAdvance,
            BlockingReasons = model.BlockingReasons
        };
    }

    private static TicketListItemDto MapListItem(ManagementTicketListItemModel model)
    {
        return new TicketListItemDto
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

    private static TicketFilterDto MapFilter(TicketFilterModel model)
    {
        return new TicketFilterDto
        {
            Search = model.Search,
            StatusId = model.StatusId,
            PriorityId = model.PriorityId,
            CategoryId = model.CategoryId,
            CustomerId = model.CustomerId,
            PropertyId = model.PropertyId,
            UnitId = model.UnitId,
            ResidentId = model.ResidentId,
            VendorId = model.VendorId,
            DueFrom = model.DueFrom,
            DueTo = model.DueTo
        };
    }

    private static TicketSelectorOptionsDto MapOptions(TicketSelectorOptionsModel model)
    {
        return new TicketSelectorOptionsDto
        {
            Statuses = model.Statuses.Select(MapOption).ToList(),
            Priorities = model.Priorities.Select(MapOption).ToList(),
            Categories = model.Categories.Select(MapOption).ToList(),
            Customers = model.Customers.Select(MapOption).ToList(),
            Properties = model.Properties.Select(MapOption).ToList(),
            Units = model.Units.Select(MapOption).ToList(),
            Residents = model.Residents.Select(MapOption).ToList(),
            Vendors = model.Vendors.Select(MapOption).ToList()
        };
    }

    private static LookupOptionDto MapOption(TicketOptionModel model)
    {
        return new LookupOptionDto
        {
            Id = model.Id,
            Label = model.Label,
            Code = model.Code
        };
    }

    private static TicketScheduledWorkListItemDto MapScheduledWork(ScheduledWorkListItemModel model)
    {
        return new TicketScheduledWorkListItemDto
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
}
