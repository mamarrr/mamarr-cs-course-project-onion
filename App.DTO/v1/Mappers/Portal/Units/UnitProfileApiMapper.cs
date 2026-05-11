using App.BLL.DTO.Units.Models;
using App.DTO.v1.Portal.Units;

namespace App.DTO.v1.Mappers.Portal.Units;

public class UnitProfileApiMapper
{
    public UnitProfileDto Map(UnitProfileModel model)
    {
        return new UnitProfileDto
        {
            UnitId = model.UnitId,
            PropertyId = model.PropertyId,
            CustomerId = model.CustomerId,
            ManagementCompanyId = model.ManagementCompanyId,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            CustomerSlug = model.CustomerSlug,
            CustomerName = model.CustomerName,
            PropertySlug = model.PropertySlug,
            PropertyName = model.PropertyName,
            UnitSlug = model.UnitSlug,
            UnitNr = model.UnitNr,
            FloorNr = model.FloorNr,
            SizeM2 = model.SizeM2,
            Notes = model.Notes,
            Path = BuildPath(model)
        };
    }

    private static string BuildPath(UnitProfileModel model)
    {
        return $"/api/v1/portal/companies/{model.CompanySlug}/customers/{model.CustomerSlug}/properties/{model.PropertySlug}/units/{model.UnitSlug}/profile";
    }
}
