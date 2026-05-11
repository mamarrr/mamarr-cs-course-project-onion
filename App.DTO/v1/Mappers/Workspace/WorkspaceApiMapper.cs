using App.BLL.DTO.Workspace.Models;
using App.DTO.v1.Workspace;

namespace App.DTO.v1.Mappers.Workspace;

public sealed class WorkspaceApiMapper
{
    public WorkspaceCatalogDto Map(UserWorkspaceCatalogModel model)
    {
        return new WorkspaceCatalogDto
        {
            ManagementCompanies = model.ManagementCompanies.Select(MapOption).ToList(),
            Customers = model.Customers.Select(MapOption).ToList(),
            Residents = model.Residents.Select(MapOption).ToList(),
            DefaultContext = model.DefaultContext is null ? null : MapOption(model.DefaultContext)
        };
    }

    public WorkspaceRedirectDto Map(WorkspaceEntryPointModel model)
    {
        return new WorkspaceRedirectDto
        {
            Destination = model.Kind.ToString(),
            CompanySlug = model.CompanySlug,
            CustomerSlug = model.CustomerSlug,
            ResidentIdCode = model.ResidentIdCode,
            Path = BuildPath(model)
        };
    }

    public WorkspaceRedirectDto Map(WorkspaceSelectionAuthorizationModel model)
    {
        return new WorkspaceRedirectDto
        {
            Destination = model.ContextType,
            CompanySlug = model.ManagementCompanySlug,
            CustomerSlug = model.CustomerSlug,
            ResidentIdCode = model.ResidentIdCode,
            Path = BuildPath(model)
        };
    }

    private static WorkspaceOptionDto MapOption(WorkspaceOptionModel option)
    {
        return new WorkspaceOptionDto
        {
            Id = option.Id,
            ContextType = option.ContextType,
            Name = option.Name,
            Slug = option.Slug,
            ManagementCompanySlug = option.ManagementCompanySlug,
            Path = BuildPath(option),
            IsDefault = option.IsDefault,
            Permissions = new WorkspaceOptionPermissionsDto
            {
                CanManageCompanyUsers = option.CanManageCompanyUsers
            }
        };
    }

    private static string BuildPath(WorkspaceOptionModel option)
    {
        return option.ContextType switch
        {
            "management" when !string.IsNullOrWhiteSpace(option.ManagementCompanySlug)
                => $"/companies/{option.ManagementCompanySlug}",
            "customer" when !string.IsNullOrWhiteSpace(option.ManagementCompanySlug)
                            && !string.IsNullOrWhiteSpace(option.Slug)
                => $"/companies/{option.ManagementCompanySlug}/customers/{option.Slug}",
            "resident" when !string.IsNullOrWhiteSpace(option.ManagementCompanySlug)
                            && !string.IsNullOrWhiteSpace(option.Slug)
                => $"/companies/{option.ManagementCompanySlug}/residents/{option.Slug}",
            _ => "/"
        };
    }

    private static string BuildPath(WorkspaceEntryPointModel model)
    {
        return model.Kind switch
        {
            WorkspaceEntryPointKind.ManagementDashboard when !string.IsNullOrWhiteSpace(model.CompanySlug)
                => $"/companies/{model.CompanySlug}",
            WorkspaceEntryPointKind.CustomerDashboard when !string.IsNullOrWhiteSpace(model.CompanySlug)
                                                  && !string.IsNullOrWhiteSpace(model.CustomerSlug)
                => $"/companies/{model.CompanySlug}/customers/{model.CustomerSlug}",
            WorkspaceEntryPointKind.ResidentDashboard when !string.IsNullOrWhiteSpace(model.CompanySlug)
                                                   && !string.IsNullOrWhiteSpace(model.ResidentIdCode)
                => $"/companies/{model.CompanySlug}/residents/{model.ResidentIdCode}",
            _ => "/"
        };
    }

    private static string BuildPath(WorkspaceSelectionAuthorizationModel model)
    {
        return model.ContextType switch
        {
            "management" when !string.IsNullOrWhiteSpace(model.ManagementCompanySlug)
                => $"/companies/{model.ManagementCompanySlug}",
            "customer" when !string.IsNullOrWhiteSpace(model.ManagementCompanySlug)
                            && !string.IsNullOrWhiteSpace(model.CustomerSlug)
                => $"/companies/{model.ManagementCompanySlug}/customers/{model.CustomerSlug}",
            "resident" when !string.IsNullOrWhiteSpace(model.ManagementCompanySlug)
                            && !string.IsNullOrWhiteSpace(model.ResidentIdCode)
                => $"/companies/{model.ManagementCompanySlug}/residents/{model.ResidentIdCode}",
            _ => "/"
        };
    }
}
