using App.BLL.Contracts.Tickets;
using App.BLL.DTO.Tickets;
using App.DAL.DTO.Tickets;
using Base.Contracts;

namespace App.BLL.Mappers.Tickets;

public class TicketBllDtoMapper : IBaseMapper<TicketBllDto, TicketDalDto>
{
    public TicketBllDto? Map(TicketDalDto? entity)
    {
        if (entity is null) return null;

        return new TicketBllDto
        {
            Id = entity.Id,
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

    public TicketDalDto? Map(TicketBllDto? entity)
    {
        if (entity is null) return null;

        return new TicketDalDto
        {
            Id = entity.Id,
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
}

