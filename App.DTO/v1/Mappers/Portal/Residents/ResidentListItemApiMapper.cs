using App.BLL.DTO.Residents.Models;
using App.DTO.v1.Portal.Residents;

namespace App.DTO.v1.Mappers.Portal.Residents;

public class ResidentListItemApiMapper
{
    public ResidentListItemDto Map(ResidentListItemModel model, string companySlug)
    {
        return new ResidentListItemDto
        {
            ResidentId = model.ResidentId,
            FirstName = model.FirstName,
            LastName = model.LastName,
            FullName = model.FullName,
            IdCode = model.IdCode,
            PreferredLanguage = model.PreferredLanguage,
            Path = BuildPath(companySlug, model.IdCode)
        };
    }

    private static string BuildPath(string companySlug, string residentIdCode)
    {
        return $"/api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/profile";
    }
}
