using App.BLL.Contracts.Leases.Models;
using App.DTO.v1.Resident;
using App.DTO.v1.Shared;
using App.DTO.v1.Unit;

namespace WebApp.Mappers.Api.Leases;

public sealed class LeaseApiMapper
{
    public ResidentUnitLeaseDto ToResidentLeaseDto(ResidentLeaseModel item)
    {
        return new ResidentUnitLeaseDto
        {
            LeaseId = item.LeaseId,
            ResidentId = item.ResidentId,
            UnitId = item.UnitId,
            PropertyId = item.PropertyId,
            PropertyName = item.PropertyName,
            PropertySlug = item.PropertySlug,
            UnitNr = item.UnitNr,
            UnitSlug = item.UnitSlug,
            LeaseRoleId = item.LeaseRoleId,
            LeaseRoleCode = item.LeaseRoleCode,
            LeaseRoleLabel = item.LeaseRoleLabel,
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            IsActive = item.IsActive,
            Notes = item.Notes
        };
    }

    public UnitTenantLeaseDto ToUnitLeaseDto(UnitLeaseModel item)
    {
        return new UnitTenantLeaseDto
        {
            LeaseId = item.LeaseId,
            ResidentId = item.ResidentId,
            UnitId = item.UnitId,
            PropertyId = item.PropertyId,
            ResidentFullName = item.ResidentFullName,
            ResidentIdCode = item.ResidentIdCode,
            LeaseRoleId = item.LeaseRoleId,
            LeaseRoleCode = item.LeaseRoleCode,
            LeaseRoleLabel = item.LeaseRoleLabel,
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            IsActive = item.IsActive,
            Notes = item.Notes
        };
    }

    public LookupOptionDto ToLookupOptionDto(LeaseRoleOptionModel option)
    {
        return new LookupOptionDto
        {
            Id = option.LeaseRoleId,
            Code = option.Code,
            Label = option.Label
        };
    }
}
