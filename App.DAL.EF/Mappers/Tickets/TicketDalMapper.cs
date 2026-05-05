using App.DAL.DTO.Tickets;
using App.Domain;
using Base.Contracts;
using Base.Domain;

namespace App.DAL.EF.Mappers.Tickets;

public class TicketDalMapper : IBaseMapper<TicketDalDto, Ticket>
{
    public TicketDalDto? Map(Ticket? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new TicketDalDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            TicketNr = entity.TicketNr,
            Title = entity.Title.ToString(),
            Description = entity.Description.ToString(),
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

    public Ticket? Map(TicketDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new Ticket
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            TicketNr = entity.TicketNr,
            Title = new LangStr(entity.Title),
            Description = new LangStr(entity.Description),
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
