using App.BLL.DTO.ScheduledWorks.Models;
using App.DTO.v1.Portal.ScheduledWork;

namespace App.DTO.v1.Mappers.Portal.ScheduledWork;

public class ScheduledWorkListItemApiMapper
{
    public ScheduledWorkListDto Map(ScheduledWorkListModel model)
    {
        return new ScheduledWorkListDto
        {
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            TicketId = model.TicketId,
            TicketNr = model.TicketNr,
            TicketTitle = model.TicketTitle,
            Path = ScheduledWorkListPath(model.CompanySlug, model.TicketId),
            Items = model.Items
                .Select(item => Map(item, model.CompanySlug, model.TicketId))
                .ToList()
        };
    }

    public ScheduledWorkListItemDto Map(
        ScheduledWorkListItemModel model,
        string companySlug,
        Guid ticketId)
    {
        var path = ScheduledWorkPath(companySlug, ticketId, model.ScheduledWorkId);

        return new ScheduledWorkListItemDto
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
            WorkLogCount = model.WorkLogCount,
            Path = path,
            WorkLogsPath = $"{path}/work-logs"
        };
    }

    internal static string ScheduledWorkPath(string companySlug, Guid ticketId, Guid scheduledWorkId)
    {
        return $"{ScheduledWorkListPath(companySlug, ticketId)}/{scheduledWorkId:D}";
    }

    internal static string ScheduledWorkListPath(string companySlug, Guid ticketId)
    {
        return $"{CompanyApiPath(companySlug)}/tickets/{ticketId:D}/scheduled-work";
    }

    private static string CompanyApiPath(string companySlug)
    {
        return $"/api/v1/portal/companies/{Segment(companySlug)}";
    }

    private static string Segment(string value)
    {
        return Uri.EscapeDataString(value);
    }
}
