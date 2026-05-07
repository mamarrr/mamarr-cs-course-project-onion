using App.BLL.DTO.Admin.Tickets;
using App.DAL.DTO.Admin.Tickets;

namespace App.BLL.Mappers.Admin;

public class AdminTicketMonitorBllMapper
{
    public AdminTicketSearchDalDto Map(AdminTicketSearchDto dto)
    {
        return new AdminTicketSearchDalDto
        {
            Company = dto.Company,
            Customer = dto.Customer,
            TicketNumber = dto.TicketNumber,
            Status = dto.Status,
            Priority = dto.Priority,
            Category = dto.Category,
            Vendor = dto.Vendor,
            CreatedFrom = dto.CreatedFrom,
            CreatedTo = dto.CreatedTo,
            DueFrom = dto.DueFrom,
            DueTo = dto.DueTo,
            OverdueOnly = dto.OverdueOnly,
            OpenOnly = dto.OpenOnly
        };
    }

    public AdminTicketListItemDto Map(AdminTicketListItemDalDto dto)
    {
        return new AdminTicketListItemDto
        {
            Id = dto.Id,
            TicketNumber = dto.TicketNumber,
            Title = dto.Title,
            CompanyName = dto.CompanyName,
            CustomerName = dto.CustomerName,
            StatusCode = dto.StatusCode,
            StatusLabel = dto.StatusLabel,
            PriorityLabel = dto.PriorityLabel,
            CategoryLabel = dto.CategoryLabel,
            VendorName = dto.VendorName,
            CreatedAt = dto.CreatedAt,
            DueAt = dto.DueAt,
            ClosedAt = dto.ClosedAt,
            IsOverdue = dto.IsOverdue
        };
    }

    public AdminTicketDetailsDto Map(AdminTicketDetailsDalDto dto)
    {
        var listItem = Map((AdminTicketListItemDalDto)dto);
        return new AdminTicketDetailsDto
        {
            Id = listItem.Id,
            TicketNumber = listItem.TicketNumber,
            Title = listItem.Title,
            CompanyName = listItem.CompanyName,
            CustomerName = listItem.CustomerName,
            StatusCode = listItem.StatusCode,
            StatusLabel = listItem.StatusLabel,
            PriorityLabel = listItem.PriorityLabel,
            CategoryLabel = listItem.CategoryLabel,
            VendorName = listItem.VendorName,
            CreatedAt = listItem.CreatedAt,
            DueAt = listItem.DueAt,
            ClosedAt = listItem.ClosedAt,
            IsOverdue = listItem.IsOverdue,
            Description = dto.Description,
            PropertyLabel = dto.PropertyLabel,
            UnitNumber = dto.UnitNumber,
            ResidentName = dto.ResidentName,
            ScheduledWorks = dto.ScheduledWorks.Select(work => new AdminScheduledWorkDto
            {
                Id = work.Id,
                VendorName = work.VendorName,
                WorkStatusLabel = work.WorkStatusLabel,
                ScheduledStart = work.ScheduledStart,
                ScheduledEnd = work.ScheduledEnd,
                RealStart = work.RealStart,
                RealEnd = work.RealEnd
            }).ToList(),
            WorkLogs = dto.WorkLogs.Select(log => new AdminWorkLogDto
            {
                Id = log.Id,
                LoggedBy = log.LoggedBy,
                CreatedAt = log.CreatedAt,
                WorkStart = log.WorkStart,
                WorkEnd = log.WorkEnd,
                Hours = log.Hours,
                MaterialCost = log.MaterialCost,
                LaborCost = log.LaborCost,
                Description = log.Description
            }).ToList()
        };
    }
}
