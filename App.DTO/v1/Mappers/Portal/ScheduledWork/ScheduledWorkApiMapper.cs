using App.BLL.DTO.ScheduledWorks;
using App.DTO.v1.Portal.ScheduledWork;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.ScheduledWork;

public class ScheduledWorkApiMapper :
    IBaseMapper<ScheduledWorkRequestDto, ScheduledWorkBllDto>
{
    public ScheduledWorkRequestDto? Map(ScheduledWorkBllDto? entity)
    {
        return entity is null
            ? null
            : new ScheduledWorkRequestDto
            {
                VendorId = entity.VendorId,
                WorkStatusId = entity.WorkStatusId,
                ScheduledStart = entity.ScheduledStart,
                ScheduledEnd = entity.ScheduledEnd,
                RealStart = entity.RealStart,
                RealEnd = entity.RealEnd,
                Notes = entity.Notes
            };
    }

    public ScheduledWorkBllDto? Map(ScheduledWorkRequestDto? entity)
    {
        return entity is null
            ? null
            : new ScheduledWorkBllDto
            {
                VendorId = entity.VendorId,
                WorkStatusId = entity.WorkStatusId,
                ScheduledStart = entity.ScheduledStart,
                ScheduledEnd = entity.ScheduledEnd,
                RealStart = entity.RealStart,
                RealEnd = entity.RealEnd,
                Notes = entity.Notes
            };
    }

    public ScheduledWorkDto Map(ScheduledWorkBllDto dto, string companySlug, Guid ticketId)
    {
        return new ScheduledWorkDto
        {
            ScheduledWorkId = dto.Id,
            VendorId = dto.VendorId,
            WorkStatusId = dto.WorkStatusId,
            ScheduledStart = dto.ScheduledStart,
            ScheduledEnd = dto.ScheduledEnd,
            RealStart = dto.RealStart,
            RealEnd = dto.RealEnd,
            Notes = dto.Notes,
            Path = ScheduledWorkPath(companySlug, ticketId, dto.Id)
        };
    }

    private static string ScheduledWorkPath(string companySlug, Guid ticketId, Guid scheduledWorkId)
    {
        return $"{ScheduledWorkListPath(companySlug, ticketId)}/{scheduledWorkId:D}";
    }

    private static string ScheduledWorkListPath(string companySlug, Guid ticketId)
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
