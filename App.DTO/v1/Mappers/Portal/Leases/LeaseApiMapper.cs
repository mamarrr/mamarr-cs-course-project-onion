using App.BLL.DTO.Leases;
using App.BLL.DTO.Leases.Models;
using App.DTO.v1.Portal.Leases;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.Leases;

public sealed class LeaseApiMapper :
    IBaseMapper<CreateResidentLeaseDto, LeaseBllDto>,
    IBaseMapper<CreateUnitLeaseDto, LeaseBllDto>,
    IBaseMapper<UpdateLeaseDto, LeaseBllDto>
{
    CreateResidentLeaseDto? IBaseMapper<CreateResidentLeaseDto, LeaseBllDto>.Map(LeaseBllDto? entity)
    {
        return entity is null
            ? null
            : new CreateResidentLeaseDto
            {
                UnitId = entity.UnitId,
                LeaseRoleId = entity.LeaseRoleId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Notes = entity.Notes
            };
    }

    LeaseBllDto? IBaseMapper<CreateResidentLeaseDto, LeaseBllDto>.Map(CreateResidentLeaseDto? entity)
    {
        return entity is null
            ? null
            : new LeaseBllDto
            {
                UnitId = entity.UnitId,
                LeaseRoleId = entity.LeaseRoleId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Notes = entity.Notes
            };
    }

    CreateUnitLeaseDto? IBaseMapper<CreateUnitLeaseDto, LeaseBllDto>.Map(LeaseBllDto? entity)
    {
        return entity is null
            ? null
            : new CreateUnitLeaseDto
            {
                ResidentId = entity.ResidentId,
                LeaseRoleId = entity.LeaseRoleId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Notes = entity.Notes
            };
    }

    LeaseBllDto? IBaseMapper<CreateUnitLeaseDto, LeaseBllDto>.Map(CreateUnitLeaseDto? entity)
    {
        return entity is null
            ? null
            : new LeaseBllDto
            {
                ResidentId = entity.ResidentId,
                LeaseRoleId = entity.LeaseRoleId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Notes = entity.Notes
            };
    }

    UpdateLeaseDto? IBaseMapper<UpdateLeaseDto, LeaseBllDto>.Map(LeaseBllDto? entity)
    {
        return entity is null
            ? null
            : new UpdateLeaseDto
            {
                LeaseRoleId = entity.LeaseRoleId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Notes = entity.Notes
            };
    }

    LeaseBllDto? IBaseMapper<UpdateLeaseDto, LeaseBllDto>.Map(UpdateLeaseDto? entity)
    {
        return entity is null
            ? null
            : new LeaseBllDto
            {
                LeaseRoleId = entity.LeaseRoleId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Notes = entity.Notes
            };
    }
}

public sealed class LeaseResponseApiMapper
{
    public LeaseDto MapForResident(
        LeaseModel model,
        string companySlug,
        string residentIdCode)
    {
        return Map(model, ResidentLeasePath(companySlug, residentIdCode, model.LeaseId));
    }

    public LeaseDto MapForResident(
        LeaseBllDto dto,
        string companySlug,
        string residentIdCode)
    {
        return Map(dto, ResidentLeasePath(companySlug, residentIdCode, dto.Id));
    }

    public LeaseDto MapForUnit(
        LeaseModel model,
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug)
    {
        return Map(model, UnitLeasePath(companySlug, customerSlug, propertySlug, unitSlug, model.LeaseId));
    }

    public LeaseDto MapForUnit(
        LeaseBllDto dto,
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug)
    {
        return Map(dto, UnitLeasePath(companySlug, customerSlug, propertySlug, unitSlug, dto.Id));
    }

    public ResidentLeaseListItemDto MapResidentLease(
        ResidentLeaseModel model,
        string companySlug,
        string residentIdCode)
    {
        return new ResidentLeaseListItemDto
        {
            LeaseId = model.LeaseId,
            ResidentId = model.ResidentId,
            UnitId = model.UnitId,
            PropertyId = model.PropertyId,
            PropertyName = model.PropertyName,
            PropertySlug = model.PropertySlug,
            UnitNr = model.UnitNr,
            UnitSlug = model.UnitSlug,
            LeaseRoleId = model.LeaseRoleId,
            LeaseRoleCode = model.LeaseRoleCode,
            LeaseRoleLabel = model.LeaseRoleLabel,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Notes = model.Notes,
            Path = ResidentLeasePath(companySlug, residentIdCode, model.LeaseId)
        };
    }

    public UnitLeaseListItemDto MapUnitLease(
        UnitLeaseModel model,
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug)
    {
        return new UnitLeaseListItemDto
        {
            LeaseId = model.LeaseId,
            ResidentId = model.ResidentId,
            UnitId = model.UnitId,
            PropertyId = model.PropertyId,
            ResidentFullName = model.ResidentFullName,
            ResidentIdCode = model.ResidentIdCode,
            LeaseRoleId = model.LeaseRoleId,
            LeaseRoleCode = model.LeaseRoleCode,
            LeaseRoleLabel = model.LeaseRoleLabel,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Notes = model.Notes,
            Path = UnitLeasePath(companySlug, customerSlug, propertySlug, unitSlug, model.LeaseId)
        };
    }

    public LeasePropertySearchResultDto Map(LeasePropertySearchResultModel model)
    {
        return new LeasePropertySearchResultDto
        {
            Properties = model.Properties.Select(property => new LeasePropertySearchItemDto
            {
                PropertyId = property.PropertyId,
                CustomerId = property.CustomerId,
                PropertySlug = property.PropertySlug,
                PropertyName = property.PropertyName,
                CustomerSlug = property.CustomerSlug,
                CustomerName = property.CustomerName,
                AddressLine = property.AddressLine,
                City = property.City,
                PostalCode = property.PostalCode
            }).ToList()
        };
    }

    public LeaseUnitOptionsDto Map(LeaseUnitOptionsModel model)
    {
        return new LeaseUnitOptionsDto
        {
            Units = model.Units.Select(unit => new LeaseUnitOptionDto
            {
                UnitId = unit.UnitId,
                UnitSlug = unit.UnitSlug,
                UnitNr = unit.UnitNr,
                FloorNr = unit.FloorNr
            }).ToList()
        };
    }

    public LeaseResidentSearchResultDto Map(LeaseResidentSearchResultModel model)
    {
        return new LeaseResidentSearchResultDto
        {
            Residents = model.Residents.Select(resident => new LeaseResidentSearchItemDto
            {
                ResidentId = resident.ResidentId,
                FullName = resident.FullName,
                IdCode = resident.IdCode
            }).ToList()
        };
    }

    public LeaseRoleOptionsDto Map(LeaseRoleOptionsModel model)
    {
        return new LeaseRoleOptionsDto
        {
            Roles = model.Roles.Select(role => new LeaseRoleOptionDto
            {
                LeaseRoleId = role.LeaseRoleId,
                Code = role.Code,
                Label = role.Label
            }).ToList()
        };
    }

    private static LeaseDto Map(LeaseModel model, string path)
    {
        return new LeaseDto
        {
            LeaseId = model.LeaseId,
            LeaseRoleId = model.LeaseRoleId,
            ResidentId = model.ResidentId,
            UnitId = model.UnitId,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Notes = model.Notes,
            Path = path
        };
    }

    private static LeaseDto Map(LeaseBllDto dto, string path)
    {
        return new LeaseDto
        {
            LeaseId = dto.Id,
            LeaseRoleId = dto.LeaseRoleId,
            ResidentId = dto.ResidentId,
            UnitId = dto.UnitId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Notes = dto.Notes,
            Path = path
        };
    }

    private static string ResidentLeasePath(
        string companySlug,
        string residentIdCode,
        Guid leaseId)
    {
        return $"{CompanyApiPath(companySlug)}/residents/{Segment(residentIdCode)}/leases/{leaseId:D}";
    }

    private static string UnitLeasePath(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        Guid leaseId)
    {
        return $"{CompanyApiPath(companySlug)}/customers/{Segment(customerSlug)}/properties/{Segment(propertySlug)}/units/{Segment(unitSlug)}/leases/{leaseId:D}";
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
