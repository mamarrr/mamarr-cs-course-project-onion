using App.BLL.DTO.Leases.Models;
using App.BLL.DTO.Leases.Queries;
using App.BLL.DTO.Residents.Models;
using App.BLL.DTO.Units.Models;
using App.DAL.DTO.Leases;

namespace App.BLL.Mappers.Leases;

public static class LeaseBllMapper
{
    public static GetResidentLeasesQuery ToResidentLeasesQuery(ResidentWorkspaceModel workspace)
    {
        return new GetResidentLeasesQuery
        {
            AppUserId = workspace.AppUserId,
            ManagementCompanyId = workspace.ManagementCompanyId,
            CompanySlug = workspace.CompanySlug,
            CompanyName = workspace.CompanyName,
            ResidentId = workspace.ResidentId,
            ResidentIdCode = workspace.ResidentIdCode,
            FullName = workspace.FullName
        };
    }

    public static GetUnitLeasesQuery ToUnitLeasesQuery(UnitWorkspaceModel workspace)
    {
        return new GetUnitLeasesQuery
        {
            AppUserId = workspace.AppUserId,
            ManagementCompanyId = workspace.ManagementCompanyId,
            CompanySlug = workspace.CompanySlug,
            CompanyName = workspace.CompanyName,
            CustomerId = workspace.CustomerId,
            CustomerSlug = workspace.CustomerSlug,
            CustomerName = workspace.CustomerName,
            PropertyId = workspace.PropertyId,
            PropertySlug = workspace.PropertySlug,
            PropertyName = workspace.PropertyName,
            UnitId = workspace.UnitId,
            UnitSlug = workspace.UnitSlug,
            UnitNr = workspace.UnitNr
        };
    }

    public static ResidentLeaseModel MapResidentLease(ResidentLeaseDalDto lease)
    {
        return new ResidentLeaseModel
        {
            LeaseId = lease.LeaseId,
            ResidentId = lease.ResidentId,
            UnitId = lease.UnitId,
            PropertyId = lease.PropertyId,
            PropertyName = lease.PropertyName,
            PropertySlug = lease.PropertySlug,
            UnitNr = lease.UnitNr,
            UnitSlug = lease.UnitSlug,
            LeaseRoleId = lease.LeaseRoleId,
            LeaseRoleCode = lease.LeaseRoleCode,
            LeaseRoleLabel = lease.LeaseRoleLabel,
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            Notes = lease.Notes
        };
    }

    public static UnitLeaseModel MapUnitLease(UnitLeaseDalDto lease)
    {
        return new UnitLeaseModel
        {
            LeaseId = lease.LeaseId,
            ResidentId = lease.ResidentId,
            UnitId = lease.UnitId,
            PropertyId = lease.PropertyId,
            ResidentFullName = lease.ResidentFullName,
            ResidentIdCode = lease.ResidentIdCode,
            LeaseRoleId = lease.LeaseRoleId,
            LeaseRoleCode = lease.LeaseRoleCode,
            LeaseRoleLabel = lease.LeaseRoleLabel,
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            Notes = lease.Notes
        };
    }

    public static LeaseModel MapLease(LeaseDetailsDalDto lease)
    {
        return new LeaseModel
        {
            LeaseId = lease.LeaseId,
            LeaseRoleId = lease.LeaseRoleId,
            ResidentId = lease.ResidentId,
            UnitId = lease.UnitId,
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            Notes = lease.Notes
        };
    }

    public static LeasePropertySearchItemModel MapProperty(LeasePropertySearchItemDalDto property)
    {
        return new LeasePropertySearchItemModel
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
        };
    }

    public static LeaseUnitOptionModel MapUnitOption(LeaseUnitOptionDalDto unit)
    {
        return new LeaseUnitOptionModel
        {
            UnitId = unit.UnitId,
            UnitSlug = unit.UnitSlug,
            UnitNr = unit.UnitNr,
            FloorNr = unit.FloorNr,
        };
    }

    public static LeaseResidentSearchItemModel MapResidentSearchItem(LeaseResidentSearchItemDalDto resident)
    {
        return new LeaseResidentSearchItemModel
        {
            ResidentId = resident.ResidentId,
            FullName = resident.FullName,
            IdCode = resident.IdCode,
        };
    }

    public static LeaseRoleOptionModel MapLeaseRole(LeaseRoleOptionDalDto role)
    {
        return new LeaseRoleOptionModel
        {
            LeaseRoleId = role.LeaseRoleId,
            Code = role.Code,
            Label = role.Label
        };
    }

}
