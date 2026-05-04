using App.BLL.Contracts.Units.Models;
using App.DAL.Contracts.DAL.Units;

namespace App.BLL.Mappers.Units;

public static class UnitBllMapper
{
    public static UnitWorkspaceModel MapWorkspace(
        Guid appUserId,
        UnitDashboardDalDto unit)
    {
        return new UnitWorkspaceModel
        {
            AppUserId = appUserId,
            ManagementCompanyId = unit.ManagementCompanyId,
            CompanySlug = unit.CompanySlug,
            CompanyName = unit.CompanyName,
            CustomerId = unit.CustomerId,
            CustomerSlug = unit.CustomerSlug,
            CustomerName = unit.CustomerName,
            PropertyId = unit.PropertyId,
            PropertySlug = unit.PropertySlug,
            PropertyName = unit.PropertyName,
            UnitId = unit.Id,
            UnitSlug = unit.Slug,
            UnitNr = unit.UnitNr
        };
    }

    public static UnitListItemModel MapListItem(UnitListItemDalDto unit)
    {
        return new UnitListItemModel
        {
            UnitId = unit.Id,
            PropertyId = unit.PropertyId,
            UnitSlug = unit.Slug,
            UnitNr = unit.UnitNr,
            FloorNr = unit.FloorNr,
            SizeM2 = unit.SizeM2
        };
    }

    public static UnitProfileModel MapProfile(UnitProfileDalDto unit)
    {
        return new UnitProfileModel
        {
            UnitId = unit.Id,
            PropertyId = unit.PropertyId,
            CustomerId = unit.CustomerId,
            ManagementCompanyId = unit.ManagementCompanyId,
            CompanySlug = unit.CompanySlug,
            CompanyName = unit.CompanyName,
            CustomerSlug = unit.CustomerSlug,
            CustomerName = unit.CustomerName,
            PropertySlug = unit.PropertySlug,
            PropertyName = unit.PropertyName,
            UnitSlug = unit.Slug,
            UnitNr = unit.UnitNr,
            FloorNr = unit.FloorNr,
            SizeM2 = unit.SizeM2,
            Notes = unit.Notes,
            IsActive = unit.IsActive
        };
    }
}
