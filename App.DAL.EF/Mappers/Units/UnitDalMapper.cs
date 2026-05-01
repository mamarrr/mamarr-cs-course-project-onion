using App.Contracts.DAL.Units;
using App.Domain;
using Base.Contracts;

namespace App.DAL.EF.Mappers.Units;

public sealed class UnitDalMapper : IMapper<UnitDalDto, Unit>
{
    public UnitDalDto? Map(Unit? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new UnitDalDto
        {
            Id = entity.Id,
            PropertyId = entity.PropertyId,
            CustomerId = entity.Property?.CustomerId ?? Guid.Empty,
            ManagementCompanyId = entity.Property?.Customer?.ManagementCompanyId ?? Guid.Empty,
            UnitNr = entity.UnitNr,
            Slug = entity.Slug,
            IsActive = entity.IsActive
        };
    }

    public UnitListItemDalDto MapListItem(Unit entity)
    {
        return new UnitListItemDalDto
        {
            Id = entity.Id,
            PropertyId = entity.PropertyId,
            UnitNr = entity.UnitNr,
            Slug = entity.Slug,
            FloorNr = entity.FloorNr,
            SizeM2 = entity.SizeM2
        };
    }

    public UnitDashboardDalDto MapDashboard(Unit entity)
    {
        return new UnitDashboardDalDto
        {
            Id = entity.Id,
            PropertyId = entity.PropertyId,
            CustomerId = entity.Property!.CustomerId,
            ManagementCompanyId = entity.Property.Customer!.ManagementCompanyId,
            CompanySlug = entity.Property.Customer.ManagementCompany!.Slug,
            CompanyName = entity.Property.Customer.ManagementCompany.Name,
            CustomerSlug = entity.Property.Customer.Slug,
            CustomerName = entity.Property.Customer.Name,
            PropertySlug = entity.Property.Slug,
            PropertyName = entity.Property.Label.ToString(),
            UnitNr = entity.UnitNr,
            Slug = entity.Slug
        };
    }

    public UnitProfileDalDto MapProfile(Unit entity)
    {
        return new UnitProfileDalDto
        {
            Id = entity.Id,
            PropertyId = entity.PropertyId,
            CustomerId = entity.Property!.CustomerId,
            ManagementCompanyId = entity.Property.Customer!.ManagementCompanyId,
            CompanySlug = entity.Property.Customer.ManagementCompany!.Slug,
            CompanyName = entity.Property.Customer.ManagementCompany.Name,
            CustomerSlug = entity.Property.Customer.Slug,
            CustomerName = entity.Property.Customer.Name,
            PropertySlug = entity.Property.Slug,
            PropertyName = entity.Property.Label.ToString(),
            UnitNr = entity.UnitNr,
            Slug = entity.Slug,
            FloorNr = entity.FloorNr,
            SizeM2 = entity.SizeM2,
            Notes = entity.Notes?.ToString(),
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    public Unit? Map(UnitDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new Unit
        {
            Id = entity.Id,
            PropertyId = entity.PropertyId,
            UnitNr = entity.UnitNr,
            Slug = entity.Slug,
            IsActive = entity.IsActive
        };
    }
}
