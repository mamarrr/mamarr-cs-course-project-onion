using App.BLL.Contracts.Residents.Commands;
using App.BLL.Contracts.Residents.Models;
using App.BLL.Contracts.Residents.Queries;
using WebApp.ViewModels.Management.Residents;
using WebApp.ViewModels.Resident;

namespace WebApp.Mappers.Mvc.Residents;

public class ResidentMvcMapper
{
    public GetResidentsQuery ToResidentsQuery(
        string companySlug,
        Guid appUserId)
    {
        return new GetResidentsQuery
        {
            CompanySlug = companySlug,
            UserId = appUserId
        };
    }

    public GetResidentProfileQuery ToResidentQuery(
        string companySlug,
        string residentIdCode,
        Guid appUserId)
    {
        return new GetResidentProfileQuery
        {
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode,
            UserId = appUserId
        };
    }

    public CreateResidentCommand ToCreateCommand(
        string companySlug,
        AddManagementResidentViewModel vm,
        Guid appUserId)
    {
        return new CreateResidentCommand
        {
            CompanySlug = companySlug,
            UserId = appUserId,
            FirstName = vm.FirstName,
            LastName = vm.LastName,
            IdCode = vm.IdCode,
            PreferredLanguage = vm.PreferredLanguage
        };
    }

    public UpdateResidentProfileCommand ToUpdateCommand(
        string companySlug,
        string residentIdCode,
        ResidentProfileEditViewModel edit,
        Guid appUserId)
    {
        return new UpdateResidentProfileCommand
        {
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode,
            UserId = appUserId,
            FirstName = edit.FirstName,
            LastName = edit.LastName,
            IdCode = edit.IdCode,
            PreferredLanguage = edit.PreferredLanguage,
        };
    }

    public DeleteResidentCommand ToDeleteCommand(
        string companySlug,
        string residentIdCode,
        ResidentProfileEditViewModel edit,
        Guid appUserId)
    {
        return new DeleteResidentCommand
        {
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode,
            UserId = appUserId,
            ConfirmationIdCode = edit.DeleteConfirmation ?? string.Empty
        };
    }

    public ResidentProfileEditViewModel ToEditViewModel(ResidentProfileModel profile)
    {
        return new ResidentProfileEditViewModel
        {
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            IdCode = profile.ResidentIdCode,
            PreferredLanguage = profile.PreferredLanguage,
        };
    }

}
