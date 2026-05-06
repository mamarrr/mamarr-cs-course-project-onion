using System.Security.Claims;
using App.BLL.DTO.Residents.Commands;
using App.BLL.DTO.Residents.Models;
using App.BLL.DTO.Residents.Queries;
using App.DTO.v1.Management;
using App.DTO.v1.Resident;
using App.DTO.v1.Shared;

namespace WebApp.Mappers.Api.Residents;

public class ResidentApiMapper
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
        CreateManagementResidentRequestDto dto,
        ClaimsPrincipal user)
    {
        return new CreateResidentCommand
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IdCode = dto.IdCode,
            PreferredLanguage = dto.PreferredLanguage
        };
    }

    public UpdateResidentProfileCommand ToUpdateCommand(
        string companySlug,
        string residentIdCode,
        UpdateResidentProfileRequestDto dto,
        ClaimsPrincipal user)
    {
        return new UpdateResidentProfileCommand
        {
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode,
            UserId = GetAppUserId(user),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IdCode = dto.IdCode,
            PreferredLanguage = dto.PreferredLanguage,
        };
    }

    public DeleteResidentCommand ToDeleteCommand(
        string companySlug,
        string residentIdCode,
        DeleteResidentProfileRequestDto dto,
        ClaimsPrincipal user)
    {
        return new DeleteResidentCommand
        {
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode,
            UserId = GetAppUserId(user),
            ConfirmationIdCode = dto.ConfirmationIdCode
        };
    }

    public ManagementResidentsResponseDto ToResidentsResponseDto(CompanyResidentsModel model)
    {
        return new ManagementResidentsResponseDto
        {
            Residents = model.Residents.Select(resident => new ManagementResidentSummaryDto
            {
                ResidentId = resident.ResidentId,
                FirstName = resident.FirstName,
                LastName = resident.LastName,
                FullName = resident.FullName,
                IdCode = resident.IdCode,
                PreferredLanguage = resident.PreferredLanguage,
                RouteContext = CreateResidentRouteContext(
                    model.CompanySlug,
                    model.CompanyName,
                    resident.IdCode,
                    resident.FullName,
                    "resident-dashboard")
            }).ToList()
        };
    }

    public CreateManagementResidentResponseDto ToCreateResponseDto(ResidentProfileModel model)
    {
        return new CreateManagementResidentResponseDto
        {
            ResidentId = model.ResidentId,
            ResidentIdCode = model.ResidentIdCode,
            RouteContext = CreateResidentRouteContext(
                model.CompanySlug,
                model.CompanyName,
                model.ResidentIdCode,
                model.FullName,
                "resident-dashboard")
        };
    }

    public ResidentDashboardResponseDto ToDashboardResponseDto(ResidentDashboardModel model)
    {
        return new ResidentDashboardResponseDto
        {
            Dashboard = new ApiDashboardDto
            {
                Title = model.Title,
                SectionLabel = model.SectionLabel,
                Widgets = model.Widgets,
                RouteContext = CreateResidentRouteContext(model.Workspace, "resident-dashboard")
            }
        };
    }

    public ResidentProfileResponseDto ToProfileResponseDto(ResidentProfileModel model)
    {
        return new ResidentProfileResponseDto
        {
            Profile = new ResidentProfileDto
            {
                ResidentId = model.ResidentId,
                ResidentIdCode = model.ResidentIdCode,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PreferredLanguage = model.PreferredLanguage,
                RouteContext = CreateResidentRouteContext(
                    model.CompanySlug,
                    model.CompanyName,
                    model.ResidentIdCode,
                    model.FullName,
                    "resident-profile")
            }
        };
    }

    public ApiRouteContextDto CreateResidentRouteContext(
        ResidentWorkspaceModel workspace,
        string currentSection)
    {
        return CreateResidentRouteContext(
            workspace.CompanySlug,
            workspace.CompanyName,
            workspace.ResidentIdCode,
            workspace.FullName,
            currentSection);
    }

    private static ApiRouteContextDto CreateResidentRouteContext(
        string companySlug,
        string companyName,
        string residentIdCode,
        string residentDisplayName,
        string currentSection)
    {
        return new ApiRouteContextDto
        {
            CompanySlug = companySlug,
            CompanyName = companyName,
            ResidentIdCode = residentIdCode,
            ResidentDisplayName = residentDisplayName,
            CurrentSection = currentSection
        };
    }

    private static Guid GetAppUserId(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : Guid.Empty;
    }
}
