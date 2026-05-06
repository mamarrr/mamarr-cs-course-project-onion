namespace App.BLL.DTO.Onboarding.Models;

public class WorkspaceCatalogModel
{
    public string ManagementCompanyName { get; init; } = "Management Workspace";
    public bool CanManageCompanyUsers { get; init; }
    public bool HasResidentContext { get; init; }
    public IReadOnlyList<WorkspaceOptionModel> ManagementCompanies { get; init; } = [];
    public IReadOnlyList<WorkspaceOptionModel> Customers { get; init; } = [];
    public WorkspaceOptionModel? Resident { get; init; }
    public WorkspaceOptionModel? DefaultContext { get; init; }
}
