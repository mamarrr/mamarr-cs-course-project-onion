using App.BLL.DTO.Units.Models;
using App.DTO.v1.Portal.Units;

namespace App.DTO.v1.Mappers.Portal.Units;

public sealed class UnitListItemApiMapper
{
    public UnitListItemDto Map(
        UnitListItemModel model,
        string companySlug,
        string customerSlug,
        string propertySlug)
    {
        return new UnitListItemDto
        {
            UnitId = model.UnitId,
            PropertyId = model.PropertyId,
            UnitSlug = model.UnitSlug,
            UnitNr = model.UnitNr,
            FloorNr = model.FloorNr,
            SizeM2 = model.SizeM2,
            Path = BuildPath(companySlug, customerSlug, propertySlug, model.UnitSlug)
        };
    }

    private static string BuildPath(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug)
    {
        return $"/api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/profile";
    }
}
