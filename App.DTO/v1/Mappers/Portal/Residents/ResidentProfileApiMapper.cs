using App.BLL.DTO.Residents.Models;
using App.DTO.v1.Portal.Residents;

namespace App.DTO.v1.Mappers.Portal.Residents;

public class ResidentProfileApiMapper
{
    public ResidentProfileDto Map(ResidentProfileModel model)
    {
        return new ResidentProfileDto
        {
            ResidentId = model.ResidentId,
            ManagementCompanyId = model.ManagementCompanyId,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            ResidentIdCode = model.ResidentIdCode,
            FirstName = model.FirstName,
            LastName = model.LastName,
            FullName = model.FullName,
            PreferredLanguage = model.PreferredLanguage,
            Path = BuildPath(model)
        };
    }

    private static string BuildPath(ResidentProfileModel model)
    {
        return $"/api/v1/portal/companies/{model.CompanySlug}/residents/{model.ResidentIdCode}/profile";
    }
}
