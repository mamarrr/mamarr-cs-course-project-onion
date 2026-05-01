using System.Security.Claims;
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
        ClaimsPrincipal user)
    {
        return new GetResidentsQuery
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user)
        };
    }

    public GetResidentProfileQuery ToResidentQuery(
        string companySlug,
        string residentIdCode,
        ClaimsPrincipal user)
    {
        return new GetResidentProfileQuery
        {
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode,
            UserId = GetAppUserId(user)
        };
    }

    public CreateResidentCommand ToCreateCommand(
        string companySlug,
        AddManagementResidentViewModel vm,
        ClaimsPrincipal user)
    {
        return new CreateResidentCommand
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
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
        ClaimsPrincipal user)
    {
        return new UpdateResidentProfileCommand
        {
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode,
            UserId = GetAppUserId(user),
            FirstName = edit.FirstName,
            LastName = edit.LastName,
            IdCode = edit.IdCode,
            PreferredLanguage = edit.PreferredLanguage,
            IsActive = edit.IsActive
        };
    }

    public DeleteResidentCommand ToDeleteCommand(
        string companySlug,
        string residentIdCode,
        ResidentProfileEditViewModel edit,
        ClaimsPrincipal user)
    {
        return new DeleteResidentCommand
        {
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode,
            UserId = GetAppUserId(user),
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
            IsActive = profile.IsActive
        };
    }

    private static Guid GetAppUserId(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : Guid.Empty;
    }
}
