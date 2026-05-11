using App.BLL.DTO.WorkLogs;
using App.BLL.DTO.WorkLogs.Models;
using App.DTO.v1.Portal.WorkLogs;

namespace App.DTO.v1.Mappers.Portal.WorkLogs;

public sealed class WorkLogListItemApiMapper
{
    public WorkLogListDto Map(WorkLogListModel model)
    {
        return new WorkLogListDto
        {
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            TicketId = model.TicketId,
            TicketNr = model.TicketNr,
            TicketTitle = model.TicketTitle,
            ScheduledWorkId = model.ScheduledWorkId,
            VendorName = model.VendorName,
            WorkStatusLabel = model.WorkStatusLabel,
            CanViewCosts = model.CanViewCosts,
            Totals = Map(model.Totals),
            Items = model.Items
                .Select(item => Map(item, model.CompanySlug, model.TicketId, model.ScheduledWorkId))
                .ToList(),
            Path = BuildListPath(model.CompanySlug, model.TicketId, model.ScheduledWorkId)
        };
    }

    public WorkLogFormDto Map(WorkLogFormModel model)
    {
        var path = model.WorkLogId.HasValue
            ? BuildWorkLogPath(model.CompanySlug, model.TicketId, model.ScheduledWorkId, model.WorkLogId.Value)
            : BuildListPath(model.CompanySlug, model.TicketId, model.ScheduledWorkId);

        return new WorkLogFormDto
        {
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            TicketId = model.TicketId,
            TicketNr = model.TicketNr,
            TicketTitle = model.TicketTitle,
            ScheduledWorkId = model.ScheduledWorkId,
            WorkLogId = model.WorkLogId,
            VendorName = model.VendorName,
            CanViewCosts = model.CanViewCosts,
            WorkStart = model.WorkStart,
            WorkEnd = model.WorkEnd,
            Hours = model.Hours,
            MaterialCost = model.MaterialCost,
            LaborCost = model.LaborCost,
            Description = model.Description,
            Path = path
        };
    }

    public WorkLogDeleteDto Map(WorkLogDeleteModel model)
    {
        return new WorkLogDeleteDto
        {
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            TicketId = model.TicketId,
            TicketNr = model.TicketNr,
            ScheduledWorkId = model.ScheduledWorkId,
            WorkLogId = model.WorkLogId,
            VendorName = model.VendorName,
            Description = model.Description,
            Path = BuildWorkLogPath(model.CompanySlug, model.TicketId, model.ScheduledWorkId, model.WorkLogId)
        };
    }

    public WorkLogDto Map(
        WorkLogBllDto model,
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId)
    {
        return new WorkLogDto
        {
            WorkLogId = model.Id,
            ScheduledWorkId = model.ScheduledWorkId,
            AppUserId = model.AppUserId,
            WorkStart = model.WorkStart,
            WorkEnd = model.WorkEnd,
            Hours = model.Hours,
            MaterialCost = model.MaterialCost,
            LaborCost = model.LaborCost,
            Description = model.Description,
            Path = BuildWorkLogPath(companySlug, ticketId, scheduledWorkId, model.Id)
        };
    }

    private static WorkLogListItemDto Map(
        WorkLogListItemModel model,
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId)
    {
        return new WorkLogListItemDto
        {
            WorkLogId = model.WorkLogId,
            AppUserId = model.AppUserId,
            AppUserName = model.AppUserName,
            WorkStart = model.WorkStart,
            WorkEnd = model.WorkEnd,
            Hours = model.Hours,
            MaterialCost = model.MaterialCost,
            LaborCost = model.LaborCost,
            Description = model.Description,
            CreatedAt = model.CreatedAt,
            Path = BuildWorkLogPath(companySlug, ticketId, scheduledWorkId, model.WorkLogId)
        };
    }

    private static WorkLogTotalsDto Map(WorkLogTotalsModel model)
    {
        return new WorkLogTotalsDto
        {
            Count = model.Count,
            Hours = model.Hours,
            MaterialCost = model.MaterialCost,
            LaborCost = model.LaborCost,
            TotalCost = model.TotalCost
        };
    }

    private static string BuildListPath(string companySlug, Guid ticketId, Guid scheduledWorkId)
    {
        return $"/api/v1/portal/companies/{companySlug}/tickets/{ticketId:D}/scheduled-work/{scheduledWorkId:D}/work-logs";
    }

    private static string BuildWorkLogPath(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        Guid workLogId)
    {
        return $"{BuildListPath(companySlug, ticketId, scheduledWorkId)}/{workLogId:D}";
    }
}
