using App.BLL.DTO.ScheduledWorks.Models;
using App.BLL.DTO.Tickets.Models;
using App.DTO.v1.Portal.ScheduledWork;
using App.DTO.v1.Shared;

namespace App.DTO.v1.Mappers.Portal.ScheduledWork;

public class ScheduledWorkDetailsApiMapper
{
    private readonly ScheduledWorkListItemApiMapper _listItemMapper = new();

    public ScheduledWorkDetailsDto Map(ScheduledWorkDetailsModel model)
    {
        var item = _listItemMapper.Map(model, model.CompanySlug, model.TicketId);

        return new ScheduledWorkDetailsDto
        {
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            TicketId = model.TicketId,
            TicketNr = model.TicketNr,
            TicketTitle = model.TicketTitle,
            ScheduledWorkId = item.ScheduledWorkId,
            VendorId = item.VendorId,
            VendorName = item.VendorName,
            WorkStatusId = item.WorkStatusId,
            WorkStatusCode = item.WorkStatusCode,
            WorkStatusLabel = item.WorkStatusLabel,
            ScheduledStart = item.ScheduledStart,
            ScheduledEnd = item.ScheduledEnd,
            RealStart = item.RealStart,
            RealEnd = item.RealEnd,
            Notes = item.Notes,
            CreatedAt = item.CreatedAt,
            WorkLogCount = item.WorkLogCount,
            Path = item.Path,
            WorkLogsPath = item.WorkLogsPath,
            ListPath = ScheduledWorkListItemApiMapper.ScheduledWorkListPath(model.CompanySlug, model.TicketId),
            EditFormPath = $"{item.Path}/form"
        };
    }

    public ScheduledWorkFormDto Map(ScheduledWorkFormModel model)
    {
        return new ScheduledWorkFormDto
        {
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            TicketId = model.TicketId,
            TicketNr = model.TicketNr,
            TicketTitle = model.TicketTitle,
            ScheduledWorkId = model.ScheduledWorkId,
            VendorId = model.VendorId,
            WorkStatusId = model.WorkStatusId,
            ScheduledStart = model.ScheduledStart,
            ScheduledEnd = model.ScheduledEnd,
            RealStart = model.RealStart,
            RealEnd = model.RealEnd,
            Notes = model.Notes,
            Vendors = MapOptions(model.Vendors),
            WorkStatuses = MapOptions(model.WorkStatuses)
        };
    }

    private static IReadOnlyList<LookupOptionDto> MapOptions(IReadOnlyList<TicketOptionModel> options)
    {
        return options
            .Select(option => new LookupOptionDto
            {
                Id = option.Id,
                Label = option.Label,
                Code = option.Code
            })
            .ToList();
    }
}
